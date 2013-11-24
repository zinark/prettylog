using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace PrettyLog.Core.DataAccess
{
    public class LogFinder
    {
        private readonly IDataContext _context;

        public LogFinder(IDataContext context)
        {
            _context = context;
        }

        public IEnumerable<LogListItemDto> Logs(string query, DateTime start, DateTime end, string[] types, string[] messages, int limit, int skip)
        {
            var db = (_context as MongoDataContext).GetDb();

            var generatedQuery = GenerateLogsQuery(query, start, end, types, messages);

            var q = db.GetCollection("logs")
                      .Find(generatedQuery)
                      .SetSortOrder(new SortByBuilder().Descending("TimeStamp"))
                      .SetFields("_id", "TimeStamp", "Type", "Message", "ThreadId", "ApplicationName", "Ip", "Host", "Url")
                      .SetLimit(limit)
                      .SetSkip(skip);


            var result = new List<LogListItemDto>();

            foreach (var i in q)
            {
                var dto = new LogListItemDto
                {
                    Id = i["_id"].AsObjectId,
                    Message = TryGetValue(i, "Message").AsString,
                    Type = TryGetValue(i, "Type").AsString,
                    TimeStamp = ToLocal(i["TimeStamp"].ToUniversalTime()),
                    ThreadId = i["ThreadId"].AsInt32,
                    ApplicationName = TryGetValue(i, "ApplicationName").AsString,
                    Ip = TryGetValue(i, "Ip").AsString,
                    Host = TryGetValue(i, "Host").AsString,
                    Url = TryGetValue(i, "Url").AsString
                };
                result.Add(dto);
            }

            return result;
        }

        public long LogsHit(string query, DateTime start, DateTime end, string[] types, string[] messages)
        {
            var db = (_context as MongoDataContext).GetDb();

            var generatedQuery = GenerateLogsQuery(query, start, end, types, messages);

            return db.GetCollection("logs").Find(generatedQuery).Count();
        }

        public LogDto GetLogDetail(string id)
        {
            var db = (_context as MongoDataContext).GetDb();
            var found = db.GetCollection("logs").FindOne(new QueryDocument(new BsonElement("_id", new BsonObjectId(id))));
            return new LogDto()
            {
                Id = found["_id"].AsObjectId,
                Message = TryGetValue(found, "Message").AsString,
                Type = TryGetValue(found, "Type").AsString,
                TimeStamp = ToLocal(found["TimeStamp"].ToUniversalTime()),
                ThreadId = found["ThreadId"].AsInt32,
                ObjectJson = GetObject(found),
                ApplicationName = TryGetValue(found, "ApplicationName").AsString,
                Ip = TryGetValue(found, "Ip").AsString,
                Host = TryGetValue(found, "Host").AsString,
                Url = TryGetValue(found, "Url").AsString
            };
        }

        public IEnumerable<FieldDensityDto> GetFieldDensity(string fieldName, string query, DateTime start, DateTime end, int limit, int skip)
        {
            BsonDocument matchQuery = BsonDocument.Parse(query);
            BsonDocument matchDate = new BsonDocument().Add("TimeStamp", new BsonDocument().Add("$gte", start.ToUniversalTime()).Add("$lte", end.ToUniversalTime()));
            BsonDocument match = new BsonDocument("$match", matchDate.Merge(matchQuery));
            
            BsonDocument loglimitQuery = new BsonDocument().Add("$limit", 10000);
            BsonDocument logskipQuery = new BsonDocument().Add("$skip", 0);

            BsonDocument resultlimitQuery = new BsonDocument().Add("$limit", limit);
            BsonDocument resultskipQuery = new BsonDocument().Add("$skip", skip);
            
            BsonDocument sortQuery = new BsonDocument().Add("$sort", new BsonDocument().Add("count", -1));

            BsonDocument groupById = new BsonDocument()
                .Add("$group",
                     new BsonDocument().Add("_id", "$" + fieldName)
                                       .Add("count", new BsonDocument().Add("$sum", 1))
                                       .Add("firstHit", new BsonDocument().Add("$min", "$TimeStamp"))
                                       .Add("lastHit", new BsonDocument().Add("$max", "$TimeStamp")));


            var sw = Stopwatch.StartNew();

            Debug.WriteLine("match : " + match.ToString ());
            Debug.WriteLine(matchQuery.ToString());
            Debug.WriteLine(loglimitQuery.ToString());
            Debug.WriteLine(logskipQuery.ToString());
            Debug.WriteLine(groupById.ToString());
            Debug.WriteLine(sortQuery.ToString());
            Debug.WriteLine(resultlimitQuery.ToString());
            Debug.WriteLine(resultskipQuery.ToString());

            IEnumerable<BsonDocument> groups = _context.Aggregate("logs", match, groupById, sortQuery, resultlimitQuery, resultskipQuery);
            Debug.WriteLine(fieldName + " : " + sw.ElapsedMilliseconds + "ms");
            var result = new List<FieldDensityDto>();

            foreach (BsonDocument group in groups)
            {
                if (!group.Contains("_id")) continue;
                if (group["_id"].IsBsonNull) continue;

                result.Add(new FieldDensityDto
                {
                    FieldName = TryGetValue(group, "_id").AsString,
                    Total = group["count"].AsInt32,
                    FirstHit = ToLocal(group["firstHit"].ToUniversalTime()),
                    LastHit = ToLocal(group["lastHit"].ToUniversalTime())
                });
            }

            
            return result;

        }

        public IEnumerable<LogDensityDto> GetLogDensity(string query, DateTime start, DateTime end, string[] types, string[] messages, int limit, int skip)
        {
            var operators = new List<BsonDocument>();
            
            var matches = new List<BsonDocument>();

            BsonDocument matchDates = new BsonDocument().Add("TimeStamp", new BsonDocument().Add("$gte", start).Add("$lte", end));
            BsonDocument matchQuery = BsonDocument.Parse(query);
            matches.Add(matchDates);
            matches.Add(matchQuery);

            if (types != null)
                if (types.Length > 0)
                {
                    BsonDocument matchQueryType = BsonDocument.Parse("{Type : '" + types[0] + "'}");
                    matches.Add(matchQueryType);
                }

            if (messages != null)
                if (messages.Length > 0)
                {
                    BsonDocument matchQueryMessage = BsonDocument.Parse("{Message : '" + messages[0] + "'}");
                    matches.Add(matchQueryMessage);
                }
            
            BsonDocument match = new BsonDocument();

            foreach (var m in matches)
            {
                match = m.Merge(match);
            }

            Debug.WriteLine(match.ToString());
            
            operators.Add(new BsonDocument("$match", match));
            operators.Add(new BsonDocument().Add("$limit", limit));
            operators.Add(new BsonDocument().Add("$skip", skip));

            bool hourly = end.Subtract(start).Ticks <= TimeSpan.FromDays(2).Ticks;

            if (!hourly)
            {
                operators.Add(new BsonDocument()
                                  .Add("$group",
                                       new BsonDocument().Add("_id", new BsonDocument()
                                                                         .Add("year", new BsonDocument().Add("$year", "$TimeStamp"))
                                                                         .Add("month", new BsonDocument().Add("$month", "$TimeStamp"))
                                                                         .Add("day", new BsonDocument().Add("$dayOfMonth", "$TimeStamp"))
                                           )
                                                         .Add("count", new BsonDocument().Add("$sum", 1)))
                    );
            }
            else
            {
                operators.Add(new BsonDocument()
                                  .Add("$group",
                                       new BsonDocument().Add("_id", new BsonDocument()
                                                                         .Add("year", new BsonDocument().Add("$year", "$TimeStamp"))
                                                                         .Add("month", new BsonDocument().Add("$month", "$TimeStamp"))
                                                                         .Add("day", new BsonDocument().Add("$dayOfMonth", "$TimeStamp"))
                                                                         .Add("hour", new BsonDocument().Add("$hour", "$TimeStamp"))
                                           )
                                                         .Add("count", new BsonDocument().Add("$sum", 1)))
                    );
            }

            //operators.Add(new BsonDocument().Add("$sort", new BsonDocument().Add("TimeStamp", -1)));
            IEnumerable<BsonDocument> groups = _context.Aggregate("logs", operators.ToArray());

            var result = new List<LogDensityDto>();
            foreach (BsonDocument group in groups)
            {
                var day = new DateTime(group["_id"]["year"].AsInt32, group["_id"]["month"].AsInt32,
                                       group["_id"]["day"].AsInt32);
                if (hourly)
                {
                    day = new DateTime(group["_id"]["year"].AsInt32, group["_id"]["month"].AsInt32, group["_id"]["day"].AsInt32, group["_id"]["hour"].AsInt32, 0, 0);
                }
                result.Add(new LogDensityDto
                {
                    Day = ToLocal(day),
                    Total = group["count"].AsInt32
                });
            }

            return result.OrderByDescending(x => x.Day);
        }

        public void GenerateData()
        {
            var db = (_context as MongoDataContext).GetDb();
            var logs = db.GetCollection("logs");

            var types = GenerateArray(new[]
            {
                "job.a", "job.b", "job.c", "web.exceptions", "web.ui", "integrations.a", "integrations.b",
                "integrations.c"
            }, 1000);

            var messages = GenerateArray(new[]
            {
                "null exception", "not found", "id is duplicated", "range is not supported", "network exception",
                "timeout", "response is not valid"
            }, 1000);

            var appNames = new[]
            {
                "job.exe",
                "w3wp.exe",
                null,
                null,
                "csc.exe",
                null,
                "host.exe"
            };

            var ips = new[]
            {
                "192.168.0.1",
                "192.168.100.90",
                "192.168.140.87",
                "192.168.0.5",
                "192.168.0.30"
            };

            var urls = new[]
            {
                "http://www.google.com",
                "http://www.yahoo.com",
                "http://www.avansas.com",
                "http://www.blessedcode.net",
                "http://www.trash.com",
                "http://www.go.com",
            };

            var objects = new object[]
            {
                new { x = 1, y = 5},
                new { y = 5},
                new { name = "jack", surname = "london" },
                new { color = "black" },
                new { size = "large", list = new [] { 1,2,3,4}},
                new
                    {
                        products = new []{ new {code = "5001", id = 1234}, new {code = "5002", id = 1235}, new {code = "5012", id = 1237}},
                        filteredWithPrices = new []{ new {code = "5001", id = 1234}, new {code = "5002", id = 1235}},
                        filteredWithSite = new []{ new {code = "5001", id = 1234}},
                        errorsCount = 1,
                        Csv = "a;b;c;d;e\r\n1;2;3;4;5;6",
                        activeProducts = new [] {"5001", "5002", "5003", "5006"}
                    }
            };

            Random r = new Random(Environment.TickCount);
            Enumerable.Range(1, 10000).ToList().ForEach(i =>
            {
                var type = types[r.Next(types.Length)];
                var message = messages[r.Next(messages.Length)];
                var obj = objects[r.Next(objects.Length)];

                logs.Insert(new LogItem()
                {
                    Message = message,
                    Type = type,
                    ThreadId = r.Next(1, 1000),
                    TimeStamp = DateTime.Now.Subtract(TimeSpan.FromHours(r.Next(0, 24 * 30))).ToUniversalTime(),
                    Object = obj,
                    ApplicationName = appNames[r.Next(appNames.Length)],
                    Host = urls[r.Next(urls.Length)],
                    Url = urls[r.Next(urls.Length)],
                    Ip = ips[r.Next(ips.Length)]
                });
            });
        }

        string[] GenerateArray(IEnumerable<string> prefixes, int i)
        {
            var result = new List<string>();

            Enumerable.Range(1, i).ToList().ForEach(x =>
            {
                foreach (var p in prefixes)
                {
                    result.Add(p + "." + x);
                }
            });

            return result.ToArray();
        }

        private static IMongoQuery GenerateLogsQuery(string query, DateTime start, DateTime end, string[] types, string[] messages)
        {
            IMongoQuery generatedQuery = new QueryDocument(BsonDocument.Parse(query));

            var qDateRange = new QueryBuilder<BsonDocument>().And
                (
                    new QueryDocument(new BsonDocument().Add("TimeStamp",
                                                             new BsonDocument().Add("$gte", TimeZoneInfo.ConvertTimeToUtc(start)))),
                    new QueryDocument(new BsonDocument().Add("TimeStamp",
                                                             new BsonDocument().Add("$lte", TimeZoneInfo.ConvertTimeToUtc(end))))
                );

            generatedQuery = new QueryBuilder<BsonDocument>().And(generatedQuery, qDateRange);

            if (types != null)
                if (types.Length > 0)
                {
                    var qTypes = new QueryDocument(new BsonDocument().Add("Type", types[0]));
                    generatedQuery = new QueryBuilder<BsonDocument>().And(generatedQuery, qTypes);
                }

            if (messages != null)
                if (messages.Length > 0)
                {
                    var qMessages = new QueryDocument(new BsonDocument().Add("Message", messages[0]));
                    generatedQuery = new QueryBuilder<BsonDocument>().And(generatedQuery, qMessages);
                }
            return generatedQuery;
        }

        static BsonValue TryGetValue(BsonDocument i, string key)
        {
            if (!i.Contains(key)) return "";
            BsonValue value = i[key];
            if (value.IsBsonNull) return "";
            return value;
        }

        static string GetObject(BsonDocument i)
        {
            if (!i.Contains("Object")) return "null";
            return i["Object"].ToJson();
        }

        static DateTime ToLocal(DateTime date)
        {
            return TimeZoneInfo.ConvertTime(date, TimeZoneInfo.Local);
        }

        public IEnumerable<MachineStatusDto> MachineStatus(string ip, DateTime start, DateTime end, int limit, int skip)
        {
            var match = new BsonDocument().Add("$match", new BsonDocument()
                .Add("Ip", ip)
                .Add("Type", "pretty.agent")
                .Add("Message", "machine status")
                .Add(new BsonDocument().Add("TimeStamp", new BsonDocument().Add("$gte", start.ToUniversalTime()).Add("$lte", end.ToUniversalTime()))));

            BsonDocument limitQuery = new BsonDocument().Add("$limit", limit);
            BsonDocument skipQuery = new BsonDocument().Add("$skip", skip);
            BsonDocument sortQuery = new BsonDocument().Add("$sort", new BsonDocument().Add("count", -1));


            BsonDocument groupId = new BsonDocument()
                .Add("ip", "$Ip")
                .Add("year", new BsonDocument().Add("$year", "$TimeStamp"))
                .Add("month", new BsonDocument().Add("$month", "$TimeStamp"))
                .Add("day", new BsonDocument().Add("$dayOfMonth", "$TimeStamp"));

            if (end.Subtract(start).Ticks <= TimeSpan.FromHours(12).Ticks)
            {
                groupId
                    .Add("hour", new BsonDocument().Add("$hour", "$TimeStamp"))
                    .Add("minute", new BsonDocument().Add("$minute", "$TimeStamp"));
            }
            else
            {
                if (end.Subtract(start).Ticks <= TimeSpan.FromDays(7).Ticks)
                    groupId.Add("hour", new BsonDocument().Add("$hour", "$TimeStamp"));

            }

            BsonDocument groupById = new BsonDocument()
                .Add("$group",
                     new BsonDocument().Add("_id", groupId)
                                       .Add("avgcpu", new BsonDocument().Add("$avg", "$Object.CpuUsage"))
                                       .Add("avgmem", new BsonDocument().Add("$min", "$Object.AvaliableMemory"))
                                       .Add("avgnet", new BsonDocument().Add("$max", "$Object.NetworkUsage")));

            var groups = _context.Aggregate("logs", match, groupById, sortQuery, limitQuery, skipQuery);

            var result = new List<MachineStatusDto>();

            foreach (BsonDocument group in groups)
            {
                if (!group.Contains("_id")) continue;
                if (group["_id"].IsBsonNull) continue;

                result.Add(new MachineStatusDto()
                {
                    On = CreateDateByFields(@group),

                    Ip = group["_id"]["ip"].AsString,
                    CPU = Math.Round(group["avgcpu"].AsDouble, 2),
                    Memory = Math.Round(group["avgmem"].AsDouble / 1024.0d, 3),
                    Network = Math.Round(group["avgnet"].AsDouble, 2)
                });
            }

            return result;
        }

        private static DateTime CreateDateByFields(BsonDocument item)
        {
            var year = TryGetValue(item["_id"].AsBsonDocument, "year").AsInt32;
            var month = TryGetValue(item["_id"].AsBsonDocument, "month").AsInt32;
            var day = TryGetValue(item["_id"].AsBsonDocument, "day").AsInt32;
            var hour = 0;
            {
                var value = TryGetValue(item["_id"].AsBsonDocument, "hour");
                if (value is BsonInt32) hour = value.AsInt32;
            }

            var minute = 0;
            {
                var value = TryGetValue(item["_id"].AsBsonDocument, "minute");
                if (value is BsonInt32) minute = value.AsInt32;
            }

            var second = 0;
            {
                var value = TryGetValue(item["_id"].AsBsonDocument, "second");
                if (value is BsonInt32) second = value.AsInt32;
            }


            return new DateTime(year, month, day, hour, minute, second);
        }

        public IEnumerable<string> MachineStatusIps(DateTime start, DateTime end)
        {
            var match = new BsonDocument().Add("$match", new BsonDocument()
                .Add("Type", "pretty.agent")
                .Add("Message", "machine status")
                .Add(new BsonDocument().Add("TimeStamp", new BsonDocument().Add("$gte", start.ToUniversalTime()).Add("$lte", end.ToUniversalTime()))));

            BsonDocument gr = new BsonDocument()
                .Add("$group",
                     new BsonDocument().Add("_id", "$Ip")
                                       .Add("q", new BsonDocument().Add("$sum", "1")));

            var groups = _context.Aggregate("logs", match, gr);

            var r = new List<string>();
            foreach (BsonDocument group in groups)
            {
                if (!group.Contains("_id")) continue;
                if (group["_id"].IsBsonNull) continue;

                r.Add(group["_id"].AsString);
            }

            return r;
        }
    }
}