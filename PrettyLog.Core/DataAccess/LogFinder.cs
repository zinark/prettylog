using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;

namespace PrettyLog.Core.DataAccess
{
    public class LogFinder
    {
        readonly IDataContext _context;

        public LogFinder(IDataContext context)
        {
            _context = context;
        }

        public IQueryable<LogItemDto> Logs(string query, DateTime start, DateTime end, string[] types, string[] messages, int limit)
        {
            var q = _context.Query<BsonDocument>("logs", query)
                            .Where(x => x["TimeStamp"] >= start)
                            .Where(x => x["TimeStamp"] <= end);

            if (types != null) q = q.Where(x => types.Contains(x["Type"].AsString));
            if (messages != null) q = q.Where(x => messages.Contains(x["Message"].AsString));

            q = q.OrderByDescending(x => x["TimeStamp"]);
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

        public IEnumerable<TypeDensityDto> GetTypes(string query, DateTime start, DateTime end)
        {
            var result = new List<TypeDensityDto>();

            var q = _context.Query<BsonDocument>("logs", query)
                .Where(x => x["TimeStamp"] >= start)
                .Where(x => x["TimeStamp"] <= end);

            foreach (var group in q.GroupBy(x => x["Type"]))
            {
                var name = group.Key.AsString;
                var total = group.Count();
                var lastHit = group.Max(x => x["TimeStamp"]).ToUniversalTime();
                var firstHit = group.Min(x => x["TimeStamp"]).ToUniversalTime();

                result.Add(new TypeDensityDto()
                {
                    Type = name,
                    Total = total,
                    LastHit = lastHit,
                    FirstHit = firstHit
                });
            }
            return result;
        }

        public IEnumerable<MessageDensityDto> GetMessages(string query, DateTime start, DateTime end)
        {
            var result = new List<MessageDensityDto>();

            var q = _context.Query<BsonDocument>("logs", query)
                .Where(x => x["TimeStamp"] >= start)
                .Where(x => x["TimeStamp"] <= end);

            foreach (var group in q.GroupBy(x => x["Message"]))
            {
                var name = group.Key.AsString;
                var total = group.Count();
                var lastHit = group.Max(x => x["TimeStamp"]).ToUniversalTime();
                var firstHit = group.Min(x => x["TimeStamp"]).ToUniversalTime();

                result.Add(new MessageDensityDto()
                {
                    Message = name,
                    Total = total,
                    LastHit = lastHit,
                    FirstHit = firstHit
                });
            }
            return result;
        }

        public IEnumerable<LogDensityDto> GetLogDensity(string query, DateTime start, DateTime end, string[] types, string[] messages)
        {
            var result = new List<LogDensityDto>();

            var q = _context.Query<BsonDocument>("logs", query)
                .Where(x => x["TimeStamp"] >= start)
                .Where(x => x["TimeStamp"] <= end);

            if (types != null) q = q.Where(x => types.Contains(x["Type"].AsString));
            if (messages != null) q = q.Where(x => messages.Contains(x["Message"].AsString));

            foreach (var group in q.GroupBy(x => x["TimeStamp"].ToUniversalTime().Date))
            {
                var day = group.Key;
                var total = group.Count();

                result.Add(new LogDensityDto()
                {
                    Day = day,
                    Total = total
                });
            }

            return result;
        }
    }
}