using System;

namespace PrettyLog.Core.DataAccess
{
    public class FieldDensityDto
    {
        public string FieldName { get; set; }
        public long Total { get; set; }
        public DateTime LastHit { get; set; }
        public DateTime FirstHit { get; set; }
    }
}