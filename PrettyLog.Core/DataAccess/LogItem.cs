using System;
using MongoDB.Bson;

namespace PrettyLog.Core.DataAccess
{
    public class LogItem
    {
        public ObjectId Id { get; set; }
        public string Type { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Message { get; set; }
        public int ThreadId { get; set; }
        public object Object { get; set; }
        public string Host { get; set; }
        public string Ip { get; set; }
        public string Url { get; set; }
        public string ApplicationName { get; set; }
        
    }
}