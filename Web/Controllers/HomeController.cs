using System;
using System.Web.Mvc;
using Web.Core.Implementations;
using Web.Core.Interfaces;
using Web.Models;

namespace Web.Controllers
{
    public class HomeController : Controller
    {
        readonly ILogDao _dao = new LogDao();

        public ActionResult Index()
        {
            var vm = new IndexViewModel()
            {
                Logs = _dao.Logs("{}", TimeSpan.FromDays(7)),
                SearchQuery = "{}",
                DayCount = 7
            };

            return View(vm);
        }

        public ActionResult Search(string searchQuery, int dayCount)
        {
            var vm = new IndexViewModel()
            {
                Logs = _dao.Logs(searchQuery, TimeSpan.FromDays(dayCount)),
                SearchQuery = searchQuery,
                DayCount = dayCount
            };

            return View("Index", vm);
        }

        public ActionResult GenerateData()
        {
            _dao.GenerateData();
            var vm = new IndexViewModel()
            {
                Logs = _dao.Logs("{}", TimeSpan.FromDays(7)),
                SearchQuery = "{}",
                DayCount = 7
            };

            return View("Index", vm);
        }
    }
}