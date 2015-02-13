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

namespace Spark.Core
{
    public static class Localhost
    {
        private static List<Uri> endpoints = new List<Uri>();

        public static Uri Endpoint {get; set; }

        public static Uri Absolute(Uri uri)
        {
            // todo: MOTOC (move to other class candidate)
            return uri.IsAbsoluteUri ? uri : new Uri(Localhost.Endpoint, uri.ToString());
        }

        public static void Add(string endpoint, bool _default = false)
        {
            Add(new Uri(endpoint, UriKind.RelativeOrAbsolute), _default);
        }

        public static void Add(Uri endpoint, bool _default = false)
        {
            endpoints.Add(endpoint);
            if (_default) Endpoint = endpoint;
        }

        public static bool HasEndpointFor(Uri uri)
        {
            return endpoints.Any(service => service.IsBaseOf(uri));
        }

        public static Uri GetEndpointOf(Uri uri)
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
