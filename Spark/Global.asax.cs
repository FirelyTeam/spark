/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Spark.Config;
using Spark.Core;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Spark
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            Settings.Init(ConfigurationManager.AppSettings);

            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            DependencyCoupler.Configure(SparkConfiguration.ConfigureDependencies);
            GlobalConfiguration.Configure(SparkConfiguration.ConfigureFhir); 

            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            
        }
    }

}
