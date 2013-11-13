using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PrettyLog.Core.DataAccess;
using Shouldly;

namespace PrettyLog.Tests
{
    [TestFixture]
    public class DataIntegrationFixture
    {
        MongoClient client = new MongoClient("mongodb://localhost");

        [Test]
        public void saving_querying_test()
        {
            using (IDataContextFactory contextFactory = new MongoDataContextFactory(client, "testDb"))
            {
                var ctx = contextFactory.Create();
                ctx.Drop("testCollection");

                ctx.Save("testCollection", new { x = 1, y = 2 }.ToBsonDocument());
                ctx.Query<BsonDocument>("testCollection").Count().ShouldBe(1);
            }
        }

        [Test]
        public void logFinder_find_test()
        {
            using (IDataContextFactory contextFactory = new MongoDataContextFactory(client, "testDb"))
            {
                IDataContext ctx = contextFactory.Create();

                GenerateLogs(ctx);
                var logFinder = new LogFinder(ctx);

                const string query = "{}";
                DateTime start = DateTime.MinValue;
                DateTime end = DateTime.MaxValue;
                var types = new[] { "job.a", "job.b", "job.c" };
                string[] messages = null;
                const int limit = 250;

                IQueryable<LogItemDto> result = logFinder.Logs(query, start, end, types, messages, limit);
                result.Count().ShouldBeGreaterThan(0);
            }
        }

        [Test]
        public void json_date_prob()
        {
            var obj = new { day = DateTime.Now };
            Console.WriteLine(JsonConvert.SerializeObject(obj, new JavaScriptDateTimeConverter()));
        }

        public void GenerateLogs(IDataContext ctx)
        {
            ctx.Drop("logs");
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
                var type = types[i % types.Length];
                var message = messages[i % messages.Length];
                var obj = objects[i % objects.Length];

                ctx.Save("logs", new LogItem()
                {
                    Message = message,
                    Type = type,
                    ThreadId = r.Next(1, 1000).ToString(),
                    TimeStamp = DateTime.Now.Subtract(TimeSpan.FromHours(r.Next(1, 3600))),
                    Object = obj
                });
            });
        }

        [Test]
        public void bson_parse_test()
        {
            var q = @"{
 id: 1,
 name: 'J-E-L-L-O',
 store:[{id: 1,
    name: 'Store X'},
    {id: 2,
    name: 'Store Y'}]
}";
            var b = BsonDocument.Parse(q);
            b.ShouldNotBe(null);
        }
    }
}
