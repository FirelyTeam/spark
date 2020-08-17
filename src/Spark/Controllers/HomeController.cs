using System.Web.Mvc;
using Spark.Store.Mongo;
using Spark.MetaStore;
using MongoDB.Driver;

namespace Spark.Controllers
{
    public class HomeController : Controller
    {
        private IMongoDatabase _db;
        public HomeController(string mongoUrl)
        {
            _db = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
        }
        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";

            return View();
        }

        public ActionResult Overview()
        {
            var store = new MetaContext(_db);
            var stats = new VmStatistics();
            stats.ResourceStats = store.GetResourceStats();

            return View(stats);
        }

    }
}
