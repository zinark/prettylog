using System;
using System.Collections.Generic;
using Web.Core.Dtos;

namespace Web.Core.Interfaces
{
    public interface ILogDao
    {
        IEnumerable<LogItemDto> Logs(string query, TimeSpan dateRange);
        void GenerateData();
    }
}