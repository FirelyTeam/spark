/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

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

<<<<<<< HEAD
        public static string Message
        {
            get
            {
                return AppSettings.Get("WELCOME");
            }
=======
        public static string Get(string key)
        {
            string s = AppSettings.Get(key);
            if (string.IsNullOrEmpty(s))
                throw new ArgumentException(string.Format("The configuration variable {0} is missing.", key));
            return s;
>>>>>>> master
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