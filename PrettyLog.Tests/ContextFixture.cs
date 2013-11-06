using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using Shouldly;
using Web.DataAccess;

namespace PrettyLog.Tests
{
    [TestFixture]
    public class ContextFixture
    {
        [Test]
        public void foo()
        {
            var client = new MongoClient("mongodb://localhost");
            
            using (IDataContextFactory contextFactory = new MongoDataContextFactoryFactory(client, "testDb"))
            {
                var ctx = contextFactory.Create();
                ctx.Drop("testCollection");
                
                ctx.Save("testCollection", new { x = 1, y = 2 }.ToBsonDocument());
                ctx.Query<BsonDocument>("testCollection").Count ().ShouldBe (1);
            }
            
        }
    }
}
