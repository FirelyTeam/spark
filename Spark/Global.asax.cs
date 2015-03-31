/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using Spark.Config;
using Spark.Core;
using Spark.Data.AmazonS3;
using Spark.Support;
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

            DependencyCoupler.Configure(ConfigureDependencies);
            GlobalConfiguration.Configure(ConfigureFhir); 

            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

        }

        public static void ConfigureFhir(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();
            config.EnableCors();
            config.AddFhir();
        }

        public static void ConfigureDependencies()
        {
            if (Config.Settings.UseS3)
            {
                DependencyCoupler.Register<IBlobStorage>(new AmazonS3Storage(Settings.AwsAccessKey, Settings.AwsSecretKey, Settings.AwsBucketName));
            }
            DependencyCoupler.Register<Conformance>(Factory.GetSparkConformance);
        }
    }

}
