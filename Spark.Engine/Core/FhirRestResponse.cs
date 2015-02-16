using Hl7.Fhir.Model;
using System;
using System.Net;
using System.Net.Http;

namespace Spark.Core
{
    public class FhirRestResponse
    {
        public HttpStatusCode StatusCode;
        public Key Key;
        public Resource Resource;

        public FhirRestResponse(HttpStatusCode code, Key key, Resource resource)
        {
            this.StatusCode = code;
            this.Key = key;
            this.Resource = resource;
        }

        public FhirRestResponse(HttpStatusCode code, Resource resource)
        {
            this.StatusCode = code;
            this.Key = Key.Null;
            this.Resource = resource;
        }

        public FhirRestResponse(HttpStatusCode code)
        {
            this.StatusCode = code;
            this.Key = Key.Null;
            this.Resource = null;
        }

    }

    public static class FhirRest
    {
        public static FhirRestResponse Error(HttpStatusCode code)
        {
            return new FhirRestResponse(code, Key.Null, null);
        }


        public static FhirRestResponse Response(HttpStatusCode code)
        {
            return new FhirRestResponse(code, null);
        }

        public static FhirRestResponse Response(int code)
        {
            return new FhirRestResponse((HttpStatusCode)code, null);
        }
        
        public static FhirRestResponse Response(int code, Resource resource)
        {
            return new FhirRestResponse((HttpStatusCode)code, null);
        }

        public static FhirRestResponse Error(HttpStatusCode code, string message, params object[] args)
        {
            OperationOutcome outcome = new OperationOutcome();
            outcome.AddError(string.Format(message, args));
            return new FhirRestResponse(code, outcome);
        }


        public static FhirRestResponse Resource(Key key, Resource resource)
        {
            return new FhirRestResponse(HttpStatusCode.OK, key, resource);
        }

        public static FhirRestResponse Resource(HttpStatusCode code, Key key, Resource resource)
        {
            return new FhirRestResponse(code, key, resource);
        }

        public static FhirRestResponse Resource(HttpStatusCode code, Entry entry)
        {
            return new FhirRestResponse(code, entry.Key, entry.Resource);
        }

        public static FhirRestResponse Resource(Entry entry)
        {
            return new FhirRestResponse(HttpStatusCode.OK, entry.Key, entry.Resource);
        }

        public static FhirRestResponse NotFound(Key key)
        {
            if (key.VersionId == null)
            {
                return FhirRest.Error(HttpStatusCode.NotFound, "No {0} resource with id {1} was found.", key.TypeName, key.ResourceId);
            }
            else
            {
                return FhirRest.Error(HttpStatusCode.NotFound, "There is no {0} resource with id {1}, or there is no version {2}", key.TypeName, key.ResourceId, key.VersionId);
            }
        }


        public static FhirRestResponse Gone(Entry entry)
        {

            var message = String.Format(
                  "A {0} resource with id {1} existed, but was deleted on {2} (version {3}).",
                  entry.Key.TypeName,
                  entry.Key.ResourceId,
                  entry.When,
                  entry.Key.ToRelativeUri());

            return FhirRest.Error(HttpStatusCode.Gone, message);
        }

    }
}