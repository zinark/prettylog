using System;
using System.Collections.Generic;
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
            var q = _context.Query<BsonDocument>("logs", query)
                .Where(x => x["TimeStamp"] >= start)
                .Where(x => x["TimeStamp"] <= end);

            if (types != null) q = q.Where(x => types.Contains(x["Type"].AsString));
            
            q = q.Take(limit);

            var projection = q.Select(i =>
                new LogItemDto
                {
                    Id = i["_id"].AsObjectId,
                    Message = i["Message"].AsString,
                    Type = i["Type"].AsString,
                    TimeStamp = i["TimeStamp"].AsDateTime,
                    Object = GetObject(i),
                    ThreadId = int.Parse(i["ThreadId"].AsString)
                });

            return projection;
        }

        static string GetObject(BsonDocument i)
        {
            if (!i.Contains("Object")) return "null";
            return i["Object"].ToJson();
        }

        public IEnumerable<TypeDto> GetTypes(string query, DateTime start, DateTime end)
        {
            var result = new List<TypeDto>();
            
            var q = _context.Query<BsonDocument>("logs", query)
                .Where(x => x["TimeStamp"] >= start)
                .Where(x => x["TimeStamp"] <= end);

            foreach (var group in q.GroupBy(x => x["Type"]))
            {
                var name = group.Key.AsString;
                var total = group.Count();
                var lastHit = group.Max(x => x["TimeStamp"]).ToUniversalTime();
                var firstHit = group.Min(x => x["TimeStamp"]).ToUniversalTime();
                
                result.Add(new TypeDto()
                {
                    Type = name,
                    Total = total,
                    LastHit = lastHit,
                    FirstHit = firstHit
                });
            }
            return result;
        }
    }
}