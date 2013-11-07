using System;

namespace Web.DataAccess
{
    public class TypeDto
    {
        public string Type { get; set; }
        public long Total { get; set; }
        public DateTime LastHit { get; set; }
        public DateTime FirstHit { get; set; }
    }
}