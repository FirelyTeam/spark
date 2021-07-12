using System.Web.Mvc;

namespace Spark.Controllers
{
    public class MaintenanceController : Controller
    {
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