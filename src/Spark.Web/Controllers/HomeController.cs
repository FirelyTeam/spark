using Microsoft.AspNetCore.Mvc;

namespace Spark.Web.Controllers
{
    public class HomeController : Controller
    {

        public IActionResult Index()
        {

            return View();
        }
    }
}
