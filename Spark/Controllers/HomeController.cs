/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Spark.Config;
using Spark.Store;
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
