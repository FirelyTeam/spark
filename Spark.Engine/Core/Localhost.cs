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
    public interface ILocalhost
    {
        Uri Base { get; }
        Uri Absolute(Uri uri);
        bool IsBaseOf(Uri uri);
        Uri GetEndpointOf(Uri uri);
    }

    public class SingleLocalhost : ILocalhost
    {
        public Uri Base { get; set; }

        public SingleLocalhost(Uri baseuri)
        {
            this.Base = baseuri;
        }

        public Uri Absolute(Uri uri)
        {
            return uri.IsAbsoluteUri ? uri : new Uri(Base, uri.ToString());
        }

        public bool IsBaseOf(Uri uri)
        {
            return Base.IsBaseOf(uri);
        }

        public Uri GetEndpointOf(Uri uri)
        {
            return (this.IsBaseOf(uri)) ? this.Base : null;
        }
    }

    /*
    public class Localhost
    {
        public Uri Base { get; set; }

        public Localhost(params Uri[] endpoints)
        {
            if (endpoints.Count() >= 1)
            {
                this.endpoints.AddRange(endpoints);
                this.Base = endpoints.First();
            }
        }

        public Localhost(params string[] endpoints)
        {
            if (endpoints.Count() >= 1)
            {
                this.endpoints.AddRange(endpoints.Select(s => new Uri(s)));
                this.Base = new Uri(endpoints.First());
            }
        }

        private List<Uri> endpoints = new List<Uri>();

        
        public Uri Absolute(Uri uri)
        {
            return uri.IsAbsoluteUri ? uri : new Uri(Base, uri.ToString());
        }

        public void Add(string endpoint, bool _default = false)
        {
            Add(new Uri(endpoint, UriKind.RelativeOrAbsolute), _default);
        }

        public void Add(Uri endpoint, bool _default = false)
        {
            endpoints.Add(endpoint);
            if (_default) Base = endpoint;
        }

        public bool IsEndpointOf(Uri uri)
        {
            return endpoints.Any(service => service.IsBaseOf(uri));
        }

        public Uri GetEndpointOf(Uri uri)
        {
            return endpoints.Find(service => service.IsBaseOf(uri));
        }
  
    }
    */
    
    public static class CommonUri
    {
        public static Uri HL7Fhir = new Uri("http://hl7.org/fhir/");
        public static Uri HL7V2 = new Uri("http://hl7.org/v2/");
    }

    
}
