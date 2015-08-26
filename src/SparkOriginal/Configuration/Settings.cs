/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Configuration;
using System.IO;

namespace Spark.Configuration
{
    public static class Settings
    {
        public static bool UseS3
        {
            get
            {
                try
                {
                    var useS3 = GetRequiredKey("FHIR_USE_S3");
                    return useS3 == "true";
                }
                catch
                {
                    return false;
                }
            }
        }

        public static int MaxBinarySize
        {
            get
            {
                try
                {
                    int max = Convert.ToInt16(GetRequiredKey("MaxBinarySize"));
                    if (max == 0) max = Int16.MaxValue;
                    return max;
                }
                catch
                {
                    return Int16.MaxValue;
                }
            }
        }

        public static string MongoUrl
        {
            get { return GetRequiredKey("MONGOLAB_URI"); }
        }

        public static string AwsAccessKey
        {
            get { return GetRequiredKey("AWSAccessKey"); }
        }

        public static string AwsSecretKey
        {
            get { return GetRequiredKey("AWSSecretKey"); }
        }

        public static string AwsBucketName
        {
            get { return GetRequiredKey("AWSBucketName"); }
        }

        public static Uri Endpoint
        {
            get 
            {
                string endpoint = GetRequiredKey("FHIR_ENDPOINT");
                return new Uri(endpoint, UriKind.Absolute); 
            }
        }
       
        public static string AuthorUri
        {
            get 
            {
                return Endpoint.Host;
            }
        }

        public static string ExamplesFile
        {
            get 
            {
                string path = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;

                if (String.IsNullOrEmpty(path))
                {
                    path = ".";
                }
            
                return Path.Combine(path, "files", "examples.zip");
            }
        }

        private static string GetRequiredKey(string key)
        {
            string s = ConfigurationManager.AppSettings.Get(key);

            if (string.IsNullOrEmpty(s))
                throw new ArgumentException(string.Format("The configuration variable {0} is missing.", key));

            return s;
        }
    }
}