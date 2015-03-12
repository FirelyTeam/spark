using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core
{
    // This class serves instances of "Response"
    public static class Respond
    {
        public static FhirResponse WithError(HttpStatusCode code)
        {
            return new FhirResponse(code, Key.Null, null);
        }

        public static FhirResponse WithCode(HttpStatusCode code)
        {
            return new FhirResponse(code, null);
        }

        public static FhirResponse WithCode(int code)
        {
            return new FhirResponse((HttpStatusCode)code, null);
        }

        public static FhirResponse WithError(HttpStatusCode code, string message, params object[] args)
        {
            OperationOutcome outcome = new OperationOutcome();
            outcome.AddError(string.Format(message, args));
            return new FhirResponse(code, outcome);
        }

        public static FhirResponse WithResource(int code, Resource resource)
        {
            return new FhirResponse((HttpStatusCode)code, resource);
        }

        public static FhirResponse WithResource(Resource resource)
        {
            return new FhirResponse(HttpStatusCode.OK, resource);
        }

        public static FhirResponse WithResource(Key key, Resource resource)
        {
            return new FhirResponse(HttpStatusCode.OK, key, resource);
        }

        public static FhirResponse WithResource(HttpStatusCode code, Key key, Resource resource)
        {
            return new FhirResponse(code, key, resource);
        }

        public static FhirResponse WithEntry(HttpStatusCode code, Interaction entry)
        {

            return new FhirResponse(code, entry.Key, entry.Resource);
        }

        public static FhirResponse WithKey(HttpStatusCode code, Key key)
        {
            return new FhirResponse(code, key, null);
        }

        public static FhirResponse WithResource(Interaction entry)
        {
            return new FhirResponse(HttpStatusCode.OK, entry.Key, entry.Resource);
        }

        public static FhirResponse NotFound(IKey key)
        {
            if (key.VersionId == null)
            {
                return Respond.WithError(HttpStatusCode.NotFound, "No {0} resource with id {1} was found.", key.TypeName, key.ResourceId);
            }
            else
            {
                return Respond.WithError(HttpStatusCode.NotFound, "There is no {0} resource with id {1}, or there is no version {2}", key.TypeName, key.ResourceId, key.VersionId);
            }
        }

        public static FhirResponse Gone(Interaction entry)
        {

            var message = String.Format(
                  "A {0} resource with id {1} existed, but was deleted on {2} (version {3}).",
                  entry.Key.TypeName,
                  entry.Key.ResourceId,
                  entry.When,
                  entry.Key.ToRelativeUri());

            return Respond.WithError(HttpStatusCode.Gone, message);
        }

        public static FhirResponse NotImplemented
        {
            get
            {
                return Respond.WithError(HttpStatusCode.NotImplemented);
            }
        }

        public static FhirResponse Success
        {
            get
            {
                return new FhirResponse(HttpStatusCode.OK);
            }
        }

        

    }
}
