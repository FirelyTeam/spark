using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Collections.Specialized;
using System.IO;

namespace Spark.Config
{
    public static class Settings
    {
        public static NameValueCollection AppSettings { get; set; }

        public static Uri Endpoint
        {
            get 
            {
                string endpoint = AppSettings.Get("FHIR_ENDPOINT");
                return new Uri(endpoint); 
            }
        }
        public static string AuthorUri
        {
            get {
                return Endpoint.Host;
            }
        }

        public static string ExamplesFile
        {
            get {
                string path = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;

                if (String.IsNullOrEmpty(path))
                {
                    path = ".";
                }
                return Path.Combine(path, "files", "examples.zip");
            }
        }
            
    }
}