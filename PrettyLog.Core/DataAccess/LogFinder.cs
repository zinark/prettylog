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

            IMongoQuery generatedQuery = new QueryDocument(BsonDocument.Parse(query));
            
            var qDateRange = new QueryBuilder<BsonDocument>().And
                (
                    new QueryDocument(new BsonDocument().Add("TimeStamp", new BsonDocument().Add("$gte", TimeZoneInfo.ConvertTimeToUtc(start)))),
                    new QueryDocument(new BsonDocument().Add("TimeStamp", new BsonDocument().Add("$lte", TimeZoneInfo.ConvertTimeToUtc(end))))
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

        static string GetStringValue(BsonDocument i, string key)
        {
            if (!i.Contains(key)) return "null";
            return i[key].AsString;
        }

        static DateTime ToLocal(DateTime date)
        {
            return TimeZoneInfo.ConvertTime(date, TimeZoneInfo.Local);
        }

        private static string GetObject(BsonDocument i)
        {
            if (!i.Contains("Object")) return "null";
            return i["Object"].ToJson();
        }

        public IEnumerable<TypeDensityDto> GetTypes(string query, DateTime start, DateTime end)
        {
            BsonDocument matchQuery = new BsonDocument().Add("$match", BsonDocument.Parse(query));

            BsonDocument matchDate = new BsonDocument()
                .Add("$match",
                     new BsonDocument().Add("TimeStamp",
                                            new BsonDocument().Add("$gte", start.ToUniversalTime())
                                                              .Add("$lte", end.ToUniversalTime())));

            BsonDocument group1 = new BsonDocument()
                .Add("$group",
                     new BsonDocument().Add("_id", "$Type")
                                       .Add("count", new BsonDocument().Add("$sum", 1))
                                       .Add("firstHit", new BsonDocument().Add("$min", "$TimeStamp"))
                                       .Add("lastHit", new BsonDocument().Add("$max", "$TimeStamp")));

            IEnumerable<BsonDocument> groups = _context.Aggregate("logs", matchQuery, matchDate, group1);

            var result = new List<TypeDensityDto>();
            foreach (BsonDocument group in groups)
            {
                result.Add(new TypeDensityDto
                {
                    Type = group["_id"].AsString,
                    Total = group["count"].AsInt32,
                    FirstHit = ToLocal(group["firstHit"].ToUniversalTime()),
                    LastHit = ToLocal(group["lastHit"].ToUniversalTime())
                });
            }

            return result;
        }

        public IEnumerable<MessageDensityDto> GetMessages(string query, DateTime start, DateTime end)
        {
            BsonDocument matchQuery = new BsonDocument().Add("$match", BsonDocument.Parse(query));

            BsonDocument matchDate = new BsonDocument()
                .Add("$match",
                     new BsonDocument().Add("TimeStamp",
                                            new BsonDocument().Add("$gte", start.ToUniversalTime())
                                                              .Add("$lte", end.ToUniversalTime())));

            BsonDocument group1 = new BsonDocument()
                .Add("$group",
                     new BsonDocument().Add("_id", "$Message")
                                       .Add("count", new BsonDocument().Add("$sum", 1))
                                       .Add("firstHit", new BsonDocument().Add("$min", "$TimeStamp"))
                                       .Add("lastHit", new BsonDocument().Add("$max", "$TimeStamp")));

            IEnumerable<BsonDocument> groups = _context.Aggregate("logs", matchQuery, matchDate, group1);

            var result = new List<MessageDensityDto>();
            foreach (BsonDocument group in groups)
            {
                result.Add(new MessageDensityDto
                {
                    Message = group["_id"].AsString,
                    Total = group["count"].AsInt32,
                    FirstHit = ToLocal(group["firstHit"].ToUniversalTime()),
                    LastHit = ToLocal(group["lastHit"].ToUniversalTime())
                });
            }

            return result;
        }

        public IEnumerable<LogDensityDto> GetLogDensity(string query, DateTime start, DateTime end, string[] types, string[] messages)
        {
            // {$group : { _id : { year : { $year : '$TimeStamp' }, month : { $month : '$TimeStamp' }, day : {$dayOfMonth : '$TimeStamp'} }, count : { $sum : 1 }       }},

            BsonDocument matchQuery = new BsonDocument().Add("$match", BsonDocument.Parse(query));

            BsonDocument matchDate = new BsonDocument()
                .Add("$match",
                     new BsonDocument().Add("TimeStamp",
                                            new BsonDocument().Add("$gte", start.ToUniversalTime())
                                                              .Add("$lte", end.ToUniversalTime())));

            BsonDocument group1 = new BsonDocument()
                .Add("$group",
                     new BsonDocument().Add("_id", new BsonDocument()
                                                       .Add("year", new BsonDocument().Add("$year", "$TimeStamp"))
                                                       .Add("month", new BsonDocument().Add("$month", "$TimeStamp"))
                                                       .Add("day", new BsonDocument().Add("$dayOfMonth", "$TimeStamp"))
                         )
                .Add("count", new BsonDocument().Add("$sum", 1)));

            IEnumerable<BsonDocument> groups = _context.Aggregate("logs", matchQuery, matchDate, group1);

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
            // var found = _context.Query<BsonDocument>("logs").FirstOrDefault(x => x["_id"] == new ObjectId(id));
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
    }
}