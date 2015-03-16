using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using Spark.Core;

namespace Spark
{
    public static class SparkConfiguration
    {
        public static void Configure(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes(); 
            config.EnableCors();
            config.AddFhir();

            
            
            
            //config.AddFhirController("fhir");
            //config.AddSecureFhirController("securefhir");
        }
    }
}