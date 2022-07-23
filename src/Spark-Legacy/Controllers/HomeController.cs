using System.Web.Mvc;
using Spark.Store.Mongo;
using Spark.MetaStore;
using MongoDB.Driver;
using Spark.Engine;

namespace Spark.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMongoDatabase _db;
        public HomeController(StoreSettings settings)
        {
            _db = MongoDatabaseFactory.GetMongoDatabase(settings.ConnectionString);
        }
        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";

            return View();
        }

        public ActionResult Overview()
        {
            var store = new MetaContext(_db);
            var stats = new VmStatistics
            {
                ResourceStats = store.GetResourceStats()
            };

            return View(stats);
        }
    }
}
