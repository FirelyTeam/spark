using Microsoft.AspNetCore.Mvc;

namespace Spark.NetCore.Controllers
{
    public class HomeController : Controller
    {

        public IActionResult Index()
        {

            return View();
        }
    }
}
