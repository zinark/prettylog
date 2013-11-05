using System;
using MongoDB.Bson;

namespace Web.Core.Dtos
{
    public class LogItemDto
    {
        public ObjectId Id { get; set; }
        public string Type { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Message { get; set; }
        public int ThreadId { get; set; }
        public string Object { get; set; }
    }
}