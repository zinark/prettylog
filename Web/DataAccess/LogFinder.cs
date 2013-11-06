using System;
using System.Linq;
using MongoDB.Bson;

namespace Web.DataAccess
{
    public class LogFinder
    {
        readonly IDataContext _context;

        public LogFinder(IDataContext context)
        {
            _context = context;
        }

        public IQueryable<LogItemDto> Find(string query, DateTime start, DateTime end, string[] types, int limit)
        {
            IQueryable<BsonDocument> q = _context.Query<BsonDocument>("logs", query);

            var r = q
                .Where(x => x["TimeStamp"] >= start)
                .Where(x => x["TimeStamp"] <= end)
                .Where(x => types.Contains(x["type"].AsString))
                .Take(limit)
                .Select(i => new LogItemDto
                {
                    Id = i["_id"].AsObjectId,
                    Message = i["Message"].AsString,
                    Type = i["Type"].AsString,
                    TimeStamp = i["TimeStamp"].AsDateTime,
                    Object = GetObject(i),
                    ThreadId = int.Parse(i["ThreadId"].AsString)
                });

            return r;
        }

        private static string GetObject(BsonDocument i)
        {
            if (!i.Contains("Object")) return "null";
            return i["Object"].ToJson();
        }

    }
}