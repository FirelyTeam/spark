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
        public static SparkException NotFound(Key key)
        {
            if (key.VersionId == null)
            {
                throw new SparkException(HttpStatusCode.NotFound, "No {1} resource with id {2} was found.", key.TypeName, key.ResourceId);
            }
            else
            {
                throw new SparkException(HttpStatusCode.NotFound, "There is no {1} resource with id {2}, or there is no version {3}", key.TypeName, key.ResourceId, key.VersionId);
            }
        }
    }
}
