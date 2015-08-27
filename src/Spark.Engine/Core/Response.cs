using Hl7.Fhir.Model;
using System.Net;
using System.Net.Http;

namespace Spark.Engine.Core
{
    // THe response class is an abstraction of the Fhir REST responses
    // This way, it's easier to implement multiple WebApi controllers
    // without having to implement functionality twice.
    // The FhirService always responds with a "Response"

    public class RespTest : HttpResponseMessage
    {

    }

    public class FhirResponse
    {
        public HttpStatusCode StatusCode;
        public IKey Key;
        public Resource Resource;

        public FhirResponse(HttpStatusCode code, IKey key, Resource resource)
        {
            this.StatusCode = code;
            this.Key = key;
            this.Resource = resource;
        }

        public FhirResponse(HttpStatusCode code, Resource resource)
        {
            this.StatusCode = code;
            this.Key = null;
            this.Resource = resource;
        }

        public FhirResponse(HttpStatusCode code)
        {
            this.StatusCode = code;
            this.Key = null;
            this.Resource = null;
        }

        public bool IsValid
        {
            get
            {
                int code = (int)this.StatusCode;
                return code <= 300;
            }
        }

        public bool HasBody
        {
            get
            {
                return Resource != null;
            }
        }

        public override string ToString()
        {
            string details = (Resource != null) ? string.Format("({0})", Resource.TypeName) : null;
            string location = Key.ToString();
            return string.Format("{0}: {1} {2} ({3})", (int)StatusCode, StatusCode.ToString(), details, location);
        }
    }

    
}