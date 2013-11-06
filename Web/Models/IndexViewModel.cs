using System.Collections.Generic;
using Web.Controllers;
using Web.DataAccess;

namespace Web.Models
{
    public class IndexViewModel
    {
        public IEnumerable<LogItemDto> Logs { get; set; }
        public string SearchQuery { get; set; }
        public int DayCount { get; set; }
    }
}