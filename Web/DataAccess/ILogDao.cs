using System;
using System.Collections.Generic;

namespace Web.DataAccess
{
    public interface ILogDao
    {
        IEnumerable<LogItemDto> Logs(string query, TimeSpan dateRange);
        void GenerateData();
    }
}