using System;
using MongoDB.Bson;

namespace Web.Controllers
{
    public class LogItem
    {
        public ObjectId Id { get; set; }
        public string Type { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Message { get; set; }
        public string ThreadId { get; set; }
        public object Object { get; set; }
    }
}