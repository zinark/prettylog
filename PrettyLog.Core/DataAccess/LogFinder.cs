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

        public IQueryable<LogItemDto> Logs(string query, DateTime start, DateTime end, string[] types, string[] messages, int limit)
        {
            //var db = (_context as MongoDataContext).GetDb();


            //var qDateRange = new QueryBuilder<BsonDocument>().And
            //    (
            //        new QueryDocument(new BsonDocument().Add("TimeStamp",new BsonDocument().Add("$gte", start.ToUniversalTime()))),
            //        new QueryDocument(new BsonDocument().Add("TimeStamp",new BsonDocument().Add("$lte", end.ToUniversalTime())))
            //    );

            //var qTypes = new QueryDocument(new BsonDocument().Add("Type", ""));

            //db.GetCollection("logs").Find(qDateRange).;

            IQueryable<BsonDocument> q = _context.Query<BsonDocument>("logs", query);

            if (types != null) q = q.Where(x => types.Contains(x["Type"].AsString));
            if (messages != null) q = q.Where(x => messages.Contains(x["Message"].AsString));

            q = q
                .Where(x => x["TimeStamp"] >= start)
                .Where(x => x["TimeStamp"] <= end);


            q = q.OrderByDescending(x => x["TimeStamp"]);
            q = q.Take(limit);
            q = q.Select(x => new BsonDocument()
                .Add("_id", x["_id"])
                .Add("Message", x["Message"])
                .Add("Type", x["Type"])
                );

            IQueryable<LogItemDto> projection = q.Select(i =>
                                                         new LogItemDto
                                                         {
                                                             Id = i["_id"].AsObjectId,
                                                             Message = i["Message"].AsString,
                                                             Type = i["Type"].AsString,
                                                             // TimeStamp = i["TimeStamp"].AsDateTime,
                                                             // ThreadId = i["ThreadId"].AsInt32
                                                         });

            return projection;
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
                    FirstHit = group["firstHit"].ToUniversalTime(),
                    LastHit = group["lastHit"].ToUniversalTime()
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
                    FirstHit = group["firstHit"].ToUniversalTime(),
                    LastHit = group["lastHit"].ToUniversalTime()
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
    }
}