/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace Spark.Core
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