using System.Collections.Generic;
using PrettyLog.Core.DataAccess;
using Web.Controllers;

namespace Web.Models
{
    public class IndexViewModel
    {
        public IEnumerable<LogListItemDto> Logs { get; set; }
        public string SearchQuery { get; set; }
        public int DayCount { get; set; }
    }
}