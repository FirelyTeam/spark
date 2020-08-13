using System.Web.Mvc;

namespace Spark.Controllers
{
    public class MaintenanceController : Controller
    {
        // GET: Maintenance
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Initialize()
        {
            return View();
        }

        public ActionResult RebuildIndex()
        {
            return View();
        }

    }
}