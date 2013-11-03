using System.Web.Mvc;
using Web.Models;

namespace Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            GenerateSample();
            var vm = new IndexViewModel()
            {
                Logs = LogDao.Logs("{}"),
                SearchQuery = "{}"
            };

            return View(vm);
        }

        public ActionResult Search(string searchQuery)
        {
            var vm = new IndexViewModel()
            {
                Logs = LogDao.Logs(searchQuery),
                SearchQuery = searchQuery
            };

            return View("Index", vm);
        }

        public ActionResult GenerateSample()
        {
            LogDao.GenerateData();
            return View("Index");
        }
    }
}