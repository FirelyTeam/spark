using Microsoft.AspNetCore.Mvc;
using Spark.Web.Services;

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
