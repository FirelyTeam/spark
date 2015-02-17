using Hl7.Fhir.Model;
using System;
using System.Net;
using System.Net.Http;

namespace Spark.Core
{
    // THe response class is an abstraction of the Fhir REST responses
    // This way, it's easier to implement multiple WebApi controllers
    // without having to implement functionality twice.
    // The FhirService always responds with a "Response"
    public class Response
    {
        public HttpStatusCode StatusCode;
        public Key Key;
        public Resource Resource;

        public Response(HttpStatusCode code, Key key, Resource resource)
        {
            this.StatusCode = code;
            this.Key = key;
            this.Resource = resource;
        }

        public Response(HttpStatusCode code, Resource resource)
        {
            this.StatusCode = code;
            this.Key = Key.Null;
            this.Resource = resource;
        }

        public Response(HttpStatusCode code)
        {
            this.StatusCode = code;
            this.Key = Key.Null;
            this.Resource = null;
        }

        public override string ToString()
        {
            string details = (Resource != null) ? string.Format("({0})", Resource.TypeName) : null;
            return string.Format("{0}: {1} {2}", (int)StatusCode, StatusCode.ToString(), details);
        }
    }

    
}