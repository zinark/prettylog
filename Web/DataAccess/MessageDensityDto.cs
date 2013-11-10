using System;

namespace Web.DataAccess
{
    public class MessageDensityDto
    {
        public string Message { get; set; }
        public long Total { get; set; }
        public DateTime FirstHit { get; set; }
        public DateTime LastHit { get; set; }
    }
}