using System;
using System.Collections.Generic;

namespace PrettyLog.Core.DataAccess
{
    public interface ILogDao
    {
        IEnumerable<LogItemDto> Logs(string query, TimeSpan dateRange);
        void GenerateData();
    }
}