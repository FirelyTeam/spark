using Microsoft.AspNetCore.Mvc;

namespace Spark.Web.Controllers
{
    public class ResourcesController : Controller
    {

        public IActionResult Index()
        {
            return View();
        }

    }
}
