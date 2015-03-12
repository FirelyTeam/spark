using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core
{
    public static class FhirResponseExtensions
    {
        public static void ApplyTo(this FhirResponse fhir, HttpResponseMessage http)
        {
            http.StatusCode = fhir.StatusCode;
            if (fhir.Key != null)
            {
                http.Headers.ETag = new EntityTagHeaderValue(fhir.Key.VersionId);
                http.Content.Headers.ContentLocation = fhir.Key.ToUri(Localhost.Base);
            }
            
            if (fhir.Resource != null && fhir.Resource.Meta != null)
            {
                http.Content.Headers.LastModified = fhir.Resource.Meta.LastUpdated;
            }
        }
    }
}
