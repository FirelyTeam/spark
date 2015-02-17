using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core
{
    // This class serves instances of "Response"
    public static class Respond
    {
        public static Response WithError(HttpStatusCode code)
        {
            return new Response(code, Key.Null, null);
        }

        public static Response WithCode(HttpStatusCode code)
        {
            return new Response(code, null);
        }

        public static Response WithCode(int code)
        {
            return new Response((HttpStatusCode)code, null);
        }

        public static Response WithError(HttpStatusCode code, string message, params object[] args)
        {
            OperationOutcome outcome = new OperationOutcome();
            outcome.AddError(string.Format(message, args));
            return new Response(code, outcome);
        }

        public static Response WithResource(int code, Resource resource)
        {
            return new Response((HttpStatusCode)code, resource);
        }

        public static Response WithResource(Resource resource)
        {
            return new Response(HttpStatusCode.OK, resource);
        }

        public static Response WithResource(Key key, Resource resource)
        {
            return new Response(HttpStatusCode.OK, key, resource);
        }

        public static Response WithResource(HttpStatusCode code, Key key, Resource resource)
        {
            return new Response(code, key, resource);
        }

        public static Response WithResource(HttpStatusCode code, Entry entry)
        {
            return new Response(code, entry.Key, entry.Resource);
        }

        public static Response WithResource(Entry entry)
        {
            return new Response(HttpStatusCode.OK, entry.Key, entry.Resource);
        }

        public static Response NotFound(Key key)
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

        public static Response Gone(Entry entry)
        {

            var message = String.Format(
                  "A {0} resource with id {1} existed, but was deleted on {2} (version {3}).",
                  entry.Key.TypeName,
                  entry.Key.ResourceId,
                  entry.When,
                  entry.Key.ToRelativeUri());

            return Respond.WithError(HttpStatusCode.Gone, message);
        }

        public static Response NotImplemented
        {
            get
            {
                return Respond.WithError(HttpStatusCode.NotImplemented);
            }
        }

        public static Response Success
        {
            get
            {
                return new Response(HttpStatusCode.OK);
            }
        }

    }
}
