using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using Spark.Core;
using Hl7.Fhir.Model;
using Spark.Support;
using Spark.Data.AmazonS3;
using Spark.Config;

namespace Spark
{
    public static class SparkConfiguration
    {
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