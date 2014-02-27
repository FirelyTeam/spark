/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Spark.Service
{
    public class SharedEndpoints
    {
        private List<Uri> endpoints = new List<Uri>();

        public void Add(string service)
        {
            endpoints.Add(new Uri(service, UriKind.RelativeOrAbsolute));
        }
        public void Add(Uri service)
        {
            endpoints.Add(service);
        }
        public bool HasEndpointFor(Uri uri)
        {
            return endpoints.Any(service => service.IsBaseOf(uri));
        }
        public Uri GetService(Uri uri)
        {
            return endpoints.Find(service => service.IsBaseOf(uri));
        }
    }

    public static class CommonUri
    {
        public static Uri HL7Fhir = new Uri("http://hl7.org/fhir/");
        public static Uri HL7V2 = new Uri("http://hl7.org/v2/");
    }
}
