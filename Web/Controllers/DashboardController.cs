using System;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;
using MongoDB.Driver;
using Web.DataAccess;

namespace Web.Controllers
{
    public class DashboardController : Controller
    {
        static readonly string Mongoconnection = ConfigurationManager.AppSettings.Get("mongoconnection");
        static readonly string Mongodatabase = ConfigurationManager.AppSettings.Get("mongodatabase");

        static readonly IDataContextFactory ContextFactory = new MongoDataContextFactory(new MongoClient(Mongoconnection), Mongodatabase);

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public JsonResult Types(string query, DateTime start, DateTime end)
        {
            var finder = new LogFinder(ContextFactory.Create());
            var types = finder.GetTypes(query, start, end);

            var rows = types.Select(x => new
            {
                c = new object[]
                {
                    new {v = x.Type},
                    new {v = x.Total},
                    new {v = x.FirstHit, f = x.FirstHit.ToString("dd/MM/yyyy hh:mm:ss")},
                    new {v = x.LastHit, f = x.LastHit.ToString("dd/MM/yyyy hh:mm:ss")}
                }
            });
            return Json(new
            {
                cols = new[]
                {
                    new {id = "Type", label = "Type", type = "string"},
                    new {id = "Total", label = "Total", type = "number"},
                    new {id = "FirstHit", label = "FirstHit", type = "date"},
                    new {id = "LastHit", label = "LastHit", type = "date"}
                },
                rows = rows
            });
        }
        
        [HttpPost]
        public JsonResult Messages(string query, DateTime start, DateTime end)
        {
            var finder = new LogFinder(ContextFactory.Create());
            var types = finder.GetMessages(query, start, end);

            var rows = types.Select(x => new
            {
                c = new object[]
                {
                    new {v = x.Message},
                    new {v = x.Total},
                    new {v = x.FirstHit, f = x.FirstHit.ToString("dd/MM/yyyy hh:mm:ss")},
                    new {v = x.LastHit, f = x.LastHit.ToString("dd/MM/yyyy hh:mm:ss")}
                }
            });
            return Json(new
            {
                cols = new[]
                {
                    new {id = "Message", label = "Message", type = "string"},
                    new {id = "Total", label = "Total", type = "number"},
                    new {id = "FirstHit", label = "FirstHit", type = "date"},
                    new {id = "LastHit", label = "LastHit", type = "date"}
                },
                rows = rows
            });
        }

        [HttpPost]
        public JsonResult Logs(string query, DateTime start, DateTime end, int limit, string[] types)
        {
            var finder = new LogFinder(ContextFactory.Create());
            var result = finder.Logs(query, start, end, types, limit);

            var rows = result.Select(x => new
            {
                c = new object[]
                {
                            new {v = x.TimeStamp, f = x.TimeStamp.ToString("dd/MM/yyyy hh:mm:ss")},
                            new {v = x.Type},
                            new {v = x.Message},
                            new {v = x.Object},
                            new {v = x.ThreadId},
                }
            });

            return Json(new
            {
                cols = new[]
                {
                    new {id = "Timestamp", label = "Timestamp", type = "date"},
                    new {id = "Type", label = "Type", type = "string"},
                    new {id = "Message", label = "Message", type = "string"},
                    new {id = "Object", label = "Object", type = "string"},
                    new {id = "ThreadId", label = "ThreadId", type = "number"},
                },
                rows = rows
            });
        }
    }
}