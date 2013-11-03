using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Web.Controllers
{
    public class LogDao
    {
        private static string mongoconnection = ConfigurationManager.AppSettings.Get("mongoconnection");
        private static string mongodatabase = ConfigurationManager.AppSettings.Get("mongodatabase");
        private static string mongocollection = ConfigurationManager.AppSettings.Get("mongocollection");

        private static readonly MongoClient client = new MongoClient(mongoconnection);

        private static readonly MongoCollection<BsonDocument> logs = client.GetServer().GetDatabase(mongodatabase).GetCollection<BsonDocument>(mongocollection);

        private static readonly TimeSpan _dateRange = TimeSpan.FromDays(365);

        public static IEnumerable<LogItemDto> Logs(string query)
        {
            logs.EnsureIndex("TimeStamp", "Type", "Message");
            logs.EnsureIndex("TimeStamp", "Message");
            
            var result = new List<LogItemDto>();

            var queryMain = new QueryDocument(BsonDocument.Parse(query));
            var queryDateFilter = Query.GTE("TimeStamp", new BsonDateTime(DateTime.Now.Subtract(_dateRange)));
            IMongoQuery q = Query.And(queryMain, queryDateFilter);
            
            foreach (BsonDocument i in logs
                .FindAs<BsonDocument>(q)
                .SetLimit(1000)
                .SetSortOrder(new SortByDocument(new
                {
                    TimeStamp = -1
                }.ToBsonDocument())))
            {
                result.Add(new LogItemDto
                {
                    Id = i["_id"].AsObjectId,
                    Message = i["Message"].AsString,
                    Type = i["Type"].AsString,
                    TimeStamp = i["TimeStamp"].AsDateTime,
                    Object = GetObject(i)
                });
            }

            return result;
        }

        private static string GetObject(BsonDocument i)
        {
            if (!i.Contains("Object")) return "null";
            return i["Object"].ToJson();
        }

        public static void GenerateData()
        {
            var types = new[]
            {
                "job.a", "job.b", "job.c", "web.exceptions", "web.ui", "integrations.a", "integrations.b", "integrations.c"
            };

            var messages = new[]
            {
                "null exception", "not found", "id is duplicated", "range is not supported", "network exception",
                "timeout", "response is not valid"
            };

            var objects = new object[]
            {
                new { x = 1, y = 5},
                new { y = 5},
                new { name = "jack", surname = "london" },
                new { color = "black" },
                new { size = "large", list = new [] { 1,2,3,4}}
            };

            Random r = new Random(Environment.TickCount);
            Enumerable.Range(1, 2000).ToList().ForEach(i =>
            {
                var type = types[i%types.Length];
                var message = messages[i%messages.Length];
                var obj = objects[i%objects.Length];

                logs.Insert(new LogItem()
                {
                    Message = message,
                    Type = type,
                    ThreadId = r.Next(1, 1000).ToString(),
                    TimeStamp = DateTime.Now.Subtract(TimeSpan.FromHours(r.Next(1, 3600))),
                    Object = obj
                });
            });
        }
    }
}