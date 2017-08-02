using System.Web.Mvc;
using MongoDB.Driver;
using Spark.MetaStore;
using Spark.Store.Mongo;

namespace Spark.Controllers
{
	public class HomeController : Controller
	{
	    private MongoDatabase db;
	    public HomeController(string mongoUrl)
	    {
	        db = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
	    }
	    public ActionResult Index()
	    {
	        ViewBag.Title = "Home Page";

	        return View();
	    }

	    public ActionResult Overview()
	    {

	        var store = new MetaContext(db);
	        var stats = new VmStatistics();
	        stats.ResourceStats = store.GetResourceStats();

	        return View(stats);
	    }

    }
}