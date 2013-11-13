using System;
using MongoDB.Bson;

namespace PrettyLog.Core.DataAccess
{
    public class LogDto
    {
        public ObjectId Id { get; set; }
        public string Type { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Message { get; set; }
        public int ThreadId { get; set; }
        public string ObjectJson { get; set; }
    }
}