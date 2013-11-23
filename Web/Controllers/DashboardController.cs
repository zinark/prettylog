using System;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PrettyLog.Core.DataAccess;

namespace Web.Controllers
{
    public class DashboardController : Controller
    {
        static readonly string Mongoconnection = ConfigurationManager.AppSettings.Get("mongoconnection");
        static readonly string Mongodatabase = ConfigurationManager.AppSettings.Get("mongodatabase");

        static readonly IDataContextFactory ContextFactory = new MongoDataContextFactory(new MongoClient(Mongoconnection), Mongodatabase);
        readonly LogFinder _finder = new LogFinder(ContextFactory.Create());
        
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Agents()
        {
            return View();
        }

        [HttpPost]
        public JsonResult MachineStatus(DateTime start, DateTime end, int limit = 200, int skip = 0)
        {
            var result = _finder.MachineStatus(start, end, limit, skip);

            var rows = result.Select(x => new
            {
                c = new object[]
                {
                            //new {v = x.Ip, f = x.Ip },
                            new {v = x.On, f = x.On.ToString("dd/MM/yyyy HH:mm:ss " + x.Ip)},
                            new {v = x.Network, f = x.Network + "%"},
                            new {v = x.CPU, f = x.CPU + "%" },
                            new {v = x.Memory, f = x.Memory + "GB"},
                }
            }).ToList();

            var json = Json(new
            {
                cols = new[]
                {
                    //new {id = "Ip", label = "Ip", type = "string"},
                    new {id = "On", label = "On", type = "datetime"},
                    new {id = "Network", label = "Network%", type = "number"},
                    new {id = "CPU", label = "CPU%", type = "number"},
                    new {id = "Memory", label = "Memory(GB)", type = "number"},
                },
                rows = rows
            });
            json.MaxJsonLength = int.MaxValue;
            return json;
        }

        [HttpPost]
        public JsonResult Logs(string query, DateTime start, DateTime end, string[] types, string[] messages, int limit, int skip = 0)
        {
            var result = _finder.Logs(query, start, end, types, messages, limit, skip);
            var hits = _finder.LogsHit(query, start, end, types, messages);

            var rows = result.Select(x => new
            {
                c = new object[]
                {
                            new {v = x.TimeStamp, f = x.TimeStamp.ToString("dd/MM/yyyy HH:mm:ss")},
                            new {v = x.Type},
                            new {v = x.Message},
                            new {v = "<a id='logId-"+x.Id+"' href='/Dashboard/Detail?id="+x.Id+"' target='_blank'> Open </a>"},
                            new {v = x.ThreadId},
                            new {v = x.Ip},
                            new {v = x.Host},
                            new {v = x.ApplicationName}
                }
            }).ToList();

            var json = Json(new
            {
                cols = new[]
                {
                    new {id = "Timestamp", label = "Timestamp", type = "datetime"},
                    new {id = "Type", label = "Type", type = "string"},
                    new {id = "Message", label = "Message", type = "string"},
                    new {id = "Object", label = "Object", type = "string"},
                    new {id = "ThreadId", label = "ThreadId", type = "number"},
                    new {id = "Ip", label = "Ip", type = "string"},
                    new {id = "Host", label = "Host", type = "string"},
                    new {id = "ApplicationName", label = "ApplicationName", type = "string"},
                },
                rows = rows,
                hits = hits
            });
            json.MaxJsonLength = int.MaxValue;
            return json;
        }

        [HttpPost]
        public JsonResult FieldDensity(string fieldName, string query, DateTime start, DateTime end, int limit = 10, int skip = 0)
        {
            var types = _finder.GetFieldDensity(fieldName, query, start, end, limit, skip);

            var rows = types.Select(x => new
            {
                c = new object[]
                {
                    new {v = x.FieldName},
                    new {v = x.Total},
                    new {v = x.FirstHit, f = x.FirstHit.ToString("dd/MM/yyyy HH:mm:ss")},
                    new {v = x.LastHit, f = x.LastHit.ToString("dd/MM/yyyy HH:mm:ss")}
                }
            });
            return Json(new
            {
                cols = new[]
                {
                    new {id = fieldName, label = fieldName, type = "string"},
                    new {id = "Total", label = "Total", type = "number"},
                    new {id = "FirstHit", label = "FirstHit", type = "datetime"},
                    new {id = "LastHit", label = "LastHit", type = "datetime"}
                },
                rows = rows
            });
        }

        public ActionResult TestData()
        {
            _finder.GenerateData();
            return View("Index");
        }

        [HttpPost]
        public ActionResult Timeline(string query, DateTime start, DateTime end, string[] types, string[] messages, int limit = 5000, int skip = 0)
        {
            var densities = _finder.GetLogDensity(query, start, end, types, messages, limit, skip);

            var rows = densities.Select(x => new
            {
                c = new object[]
                {
                    new {v = x.Day, f = x.Day.ToString("dd/MM/yyyy HH:mm:ss")},
                    new {v = x.Total},
                }
            });
            var obj = new
            {
                cols = new[]
                {
                    new {id = "Day", label = "Day", type = "datetime"},
                    new {id = "Total", label = "Total", type = "number"},
                },
                rows = rows
            };

            var json = JsonConvert.SerializeObject(obj, new IsoDateTimeConverter()); //new JavaScriptDateTimeConverter()
            return new ContentResult()
            {
                Content = json,
                ContentEncoding = Encoding.UTF8,
                ContentType = "application/json"
            };
        }

        public ActionResult Detail(string id)
        {
            LogDto dto = _finder.GetLogDetail(id);
            return View(dto);
        }

    }
}