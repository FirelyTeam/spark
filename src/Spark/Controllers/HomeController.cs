/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Spark.Configuration;
using System.Web.Mvc;
using Spark.Store.Mongo;
using Spark.MetaStore;

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
            var db = MongoInfrastructureFactory.GetMongoDatabase(Settings.MongoUrl);
            var store = new MetaContext(db);
            var stats = new VmStatistics();
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
