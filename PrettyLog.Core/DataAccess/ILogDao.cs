using System;
using System.Collections.Generic;

namespace PrettyLog.Core.DataAccess
{
    public interface ILogDao
    {
        IEnumerable<LogListItemDto> Logs(string query, TimeSpan dateRange);
        void GenerateData();
    }
}