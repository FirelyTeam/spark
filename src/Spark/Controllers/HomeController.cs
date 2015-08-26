using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Spark.Store.Mongo;
using Spark.MetaStore;

namespace Spark.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";

            return View();
        }

        public ActionResult Overview()
        {
            var db = MongoInfrastructureFactory.GetMongoDatabase(Settings.MongoUrl);
            var store = new MetaContext(db);
            var stats = new VmStatistics();
            stats.ResourceStats = store.GetResourceStats();

            return View(stats);
        }

    }
}
