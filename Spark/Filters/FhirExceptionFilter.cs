/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Filters;
using Spark.Support;
using System.Net.Http.Headers;
using System.Web.Http;
using Spark.Service;
using Spark.Core;

namespace Spark.Filters
{
    public class FhirExceptionFilter : ExceptionFilterAttribute
    {
        OperationOutcome CreateOutcome(string message)
        {
            return new OperationOutcome().Error(message);
        }
        
        OperationOutcome CreateOutcome(Exception exception)
        {
            OperationOutcome outcome = new OperationOutcome().Init();
            Exception e = exception;
            do
            {
                outcome.Error(e);
                e = e.InnerException;
            }
            while (e != null);

            return outcome;
        }

        public override void OnException(HttpActionExecutedContext context)
        {
            HttpResponseMessage errorResponse;

            if (context.Exception is SparkException)
            {
                var exception = (SparkException)context.Exception;
                var outcome = exception.Outcome == null ? CreateOutcome(exception) : exception.Outcome;
                
                errorResponse = context.Request.CreateResponse(exception.StatusCode, outcome);
            }
            else if (context.Exception is HttpResponseException)
            {
                var he = (HttpResponseException)context.Exception;
                errorResponse = context.Request.CreateResponse(he.Response.StatusCode, CreateOutcome(he.Response.ToString()));
            }
            else
                errorResponse = context.Request.CreateResponse(HttpStatusCode.InternalServerError, CreateOutcome(context.Exception));

            throw new HttpResponseException(errorResponse);
        }
    }
}