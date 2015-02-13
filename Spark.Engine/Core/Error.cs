using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core
{
    public static class Error
    {
        public static SparkException NotFound(Key key, string msg = null)
        {
            if (key.VersionId == null)
            {
                return new SparkException(HttpStatusCode.NotFound, "No {0} resource with id {1} was found.", key.TypeName, key.ResourceId);
            }
            else
            {
                return new SparkException(HttpStatusCode.NotFound, "There is no {0} resource with id {1}, or there is no version {2}", key.TypeName, key.ResourceId, key.VersionId);
            }
        }

        public static SparkException Gone(Entry deletedEntry)
        {
            
            var message = String.Format(
                  "A {0} resource with id {1} existed, but was deleted on {2} (version {3}).",
                  deletedEntry.Key.TypeName,
                  deletedEntry.Key.ResourceId,
                  deletedEntry.When,
                  deletedEntry.Key.ToRelativeUri());

            return new SparkException(HttpStatusCode.Gone, message);
        }

        public static SparkException Create(HttpStatusCode code, string message, params object[] values)
        {
            return new SparkException(code, message, values);
        }
    }
}
