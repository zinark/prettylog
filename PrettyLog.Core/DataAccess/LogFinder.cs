using System;
using System.Collections.Generic;
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

        public IEnumerable<LogListItemDto> Logs(string query, DateTime start, DateTime end, string[] types, string[] messages, int limit)
        {
            var db = (_context as MongoDataContext).GetDb();

            var generatedQuery = GenerateLogsQuery(query, start, end, types, messages);

            var q = db.GetCollection("logs")
                      .Find(generatedQuery)
                      .SetSortOrder(new SortByBuilder().Descending("TimeStamp"))
                      .SetFields("_id", "TimeStamp", "Type", "Message", "ThreadId", "ApplicationName", "Ip", "Host", "Url")
                      .SetLimit(limit);


            var result = new List<LogListItemDto>();
            
            foreach (var i in q)
            {
                var dto = new LogListItemDto
                {
                    Id = i["_id"].AsObjectId,
                    Message = i["Message"].AsString,
                    Type = i["Type"].AsString,
                    TimeStamp = ToLocal(i["TimeStamp"].ToUniversalTime()),
                    ThreadId = i["ThreadId"].AsInt32,
                    ApplicationName = GetStringValue(i, "ApplicationName"),
                    Ip = GetStringValue(i, "Ip"),
                    Host = GetStringValue(i, "Host"),
                    Url = GetStringValue(i, "Url")
                };
                result.Add(dto);
            }

            return result;
        }

        private static IMongoQuery GenerateLogsQuery(string query, DateTime start, DateTime end, string[] types,
                                                     string[] messages)
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

        static string GetStringValue(BsonDocument i, string key)
        {
            if (!i.Contains(key)) return "";
            if (i[key].IsBsonNull) return "";
            return i[key].AsString;
        }

        static DateTime ToLocal(DateTime date)
        {
            return TimeZoneInfo.ConvertTime(date, TimeZoneInfo.Local);
        }

        static string GetObject(BsonDocument i)
        {
            if (!i.Contains("Object")) return "null";
            return i["Object"].ToJson();
        }

        public IEnumerable<FieldDensityDto> GetFieldDensity(string fieldName, string query, DateTime start, DateTime end)
        {
            BsonDocument matchQuery = new BsonDocument().Add("$match", BsonDocument.Parse(query));

            BsonDocument limitQuery = new BsonDocument().Add("$limit", 1000);

            BsonDocument matchDate = new BsonDocument()
                .Add("$match",
                     new BsonDocument().Add("TimeStamp",
                                            new BsonDocument().Add("$gte", start)
                                                              .Add("$lte", end)));

            BsonDocument group1 = new BsonDocument()
                .Add("$group",
                     new BsonDocument().Add("_id", "$" + fieldName)
                                       .Add("count", new BsonDocument().Add("$sum", 1))
                                       .Add("firstHit", new BsonDocument().Add("$min", "$TimeStamp"))
                                       .Add("lastHit", new BsonDocument().Add("$max", "$TimeStamp")));

            IEnumerable<BsonDocument> groups = _context.Aggregate("logs", limitQuery, matchQuery, matchDate, group1);

            var result = new List<FieldDensityDto>();
            foreach (BsonDocument group in groups)
            {
                result.Add(new FieldDensityDto
                {
                    FieldName = group["_id"].AsString,
                    Total = group["count"].AsInt32,
                    FirstHit = ToLocal(group["firstHit"].ToUniversalTime()),
                    LastHit = ToLocal(group["lastHit"].ToUniversalTime())
                });
            }

            return result;

        }

        public IEnumerable<LogDensityDto> GetLogDensity(string query, DateTime start, DateTime end, string[] types, string[] messages)
        {
            var operators = new List<BsonDocument>();

            operators.Add(new BsonDocument().Add("$limit", 1000));
            operators.Add(new BsonDocument().Add("$match", BsonDocument.Parse(query)));
            
            if (types != null)
                if (types.Length > 0)
                {
                    BsonDocument matchQueryType = new BsonDocument().Add("$match", BsonDocument.Parse("{Type : '" + types[0] + "'}"));
                    operators.Add(matchQueryType);
                }
            
            if (messages != null)
                if (messages.Length > 0)
                {
                    BsonDocument matchQueryMessage = new BsonDocument().Add("$match", BsonDocument.Parse("{Message : '" + messages[0] + "'}"));
                    operators.Add(matchQueryMessage);
                }

            operators.Add(
                new BsonDocument()
                    .Add("$match",
                         new BsonDocument().Add("TimeStamp",
                                                new BsonDocument().Add("$gte", start)
                                                                  .Add("$lte", end)))
                );

            operators.Add(new BsonDocument()
                .Add("$group",
                     new BsonDocument().Add("_id", new BsonDocument()
                                                       .Add("year", new BsonDocument().Add("$year", "$TimeStamp"))
                                                       .Add("month", new BsonDocument().Add("$month", "$TimeStamp"))
                                                       .Add("day", new BsonDocument().Add("$dayOfMonth", "$TimeStamp"))
                         )
                .Add("count", new BsonDocument().Add("$sum", 1)))
                );

            IEnumerable<BsonDocument> groups = _context.Aggregate("logs", operators.ToArray());

            var result = new List<LogDensityDto>();
            foreach (BsonDocument group in groups)
            {
                result.Add(new LogDensityDto
                {
                    Day = new DateTime(group["_id"]["year"].AsInt32, group["_id"]["month"].AsInt32,group["_id"]["day"].AsInt32),
                    Total = group["count"].AsInt32
                });
            }

            return result;
        }

        public LogDto GetLogDetail(string id)
        {
            var db = (_context as MongoDataContext).GetDb();
            var found = db.GetCollection("logs").FindOne(new QueryDocument(new BsonElement("_id", new BsonObjectId(id))));
            return new LogDto()
            {
                Id = found["_id"].AsObjectId,
                Message = found["Message"].AsString,
                Type = found["Type"].AsString,
                TimeStamp = ToLocal(found["TimeStamp"].ToUniversalTime()),
                ThreadId = found["ThreadId"].AsInt32,
                ObjectJson = GetObject(found),
                ApplicationName = GetStringValue(found, "ApplicationName"),
                Ip = GetStringValue(found, "Ip"),
                Host = GetStringValue(found, "Host"),
                Url = GetStringValue(found, "Url")
            };
        }

        public void GenerateData()
        {
            var db = (_context as MongoDataContext).GetDb();
            var logs = db.GetCollection("logs");

            var types = new[]
            {
                "job.a", "job.b", "job.c", "web.exceptions", "web.ui", "integrations.a", "integrations.b", "integrations.c"
            };

            var messages = new[]
            {
                "null exception", "not found", "id is duplicated", "range is not supported", "network exception",
                "timeout", "response is not valid"
            };
            
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
            Enumerable.Range(1, 100000).ToList().ForEach(i =>
            {
                var type = types[r.Next(types.Length)];
                var message = messages[r.Next(messages.Length)];
                var obj = objects[r.Next(objects.Length)];

                logs.Insert(new LogItem()
                {
                    Message = message,
                    Type = type,
                    ThreadId = r.Next(1, 1000),
                    TimeStamp = DateTime.Now.Subtract(TimeSpan.FromHours(r.Next(1, 3600))),
                    Object = obj,
                    ApplicationName = appNames[r.Next(appNames.Length)],
                    Host = urls[r.Next(urls.Length)],
                    Url = urls[r.Next(urls.Length)],
                    Ip = ips[r.Next(ips.Length)]
                });
            });
        }

        public long LogsHit(string query, DateTime start, DateTime end, string[] types, string[] messages, int limit)
        {
             var db = (_context as MongoDataContext).GetDb();

            var generatedQuery = GenerateLogsQuery(query, start, end, types, messages);

            return db.GetCollection("logs").Find(generatedQuery).Count();
        }
    }
}