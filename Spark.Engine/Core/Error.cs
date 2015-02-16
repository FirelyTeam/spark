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
        

        public static SparkException Create(HttpStatusCode code, string message, params object[] values)
        {
            return new SparkException(code, message, values);
        }
    }
}
