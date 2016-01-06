/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Filters;
using Spark.Engine.ExceptionHandling;

namespace Spark.Filters
{
    public class FhirExceptionFilter : ExceptionFilterAttribute
    {
        private readonly IExceptionResponseMessageFactory exceptionResponseMessageFactory;

        public FhirExceptionFilter(IExceptionResponseMessageFactory exceptionResponseMessageFactory)
        {
            this.exceptionResponseMessageFactory = exceptionResponseMessageFactory;
        }

        public override void OnException(HttpActionExecutedContext context)
        {
            HttpResponseMessage response = exceptionResponseMessageFactory.GetResponseMessage(context.Exception, context.Request);
           
            throw new HttpResponseException(response);
        }
    }
}