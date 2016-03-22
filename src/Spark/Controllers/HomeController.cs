using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Spark.Store.Mongo;
using Spark.MetaStore;
using MongoDB.Driver;
using Microsoft.Practices.Unity;

namespace Spark.Controllers
{
    public class HomeController : Controller
    {
        //private string mongoUrl;
        //[Dependency]
        //public string MongoUrl { private get { return mongoUrl; } set { mongoUrl = value; } }

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
