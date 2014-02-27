using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace Spark.Support
{
    public class SparkException : Exception
    {
        public HttpStatusCode StatusCode;

        public SparkException(HttpStatusCode statuscode, string message = null) : base(message)
        {
            this.StatusCode = statuscode;
        }
        
        public SparkException(HttpStatusCode statuscode, string message, params object[] values)
            : base(string.Format(message, values))
        {
            this.StatusCode = statuscode;
        }
        
        public SparkException(string message) : base(message)
        {
            this.StatusCode = HttpStatusCode.BadRequest;
        }

        public SparkException(HttpStatusCode statuscode, string message, Exception inner) : base(message, inner)
        {
            this.StatusCode = statuscode;
        }
    }
}