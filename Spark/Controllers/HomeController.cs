using Spark.Config;
using Spark.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;

namespace Spark.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Conformance()
        {
            return View();
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult Overview()
        {
            var store = new MetaStore();
            var stats = new Stats();
            stats.ResourceStats = store.GetResourceStats();
            
            return View(stats);
        }

        public ActionResult Examples()
        {
            return View();
        }

        public ActionResult API()
        {
            return View();
        }

        public ActionResult Contact()
        {
            return View();
        }
    }
}
