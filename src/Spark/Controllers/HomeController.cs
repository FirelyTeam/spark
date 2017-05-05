using System.Web.Mvc;

namespace Spark.Controllers
{
	public class HomeController : Controller
	{
		public HomeController()
		{
		}

		public ActionResult Index()
		{
			ViewBag.Title = "Home Page";

			return View();
		}
	}
}