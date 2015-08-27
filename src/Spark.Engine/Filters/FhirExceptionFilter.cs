/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using System;
using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;
using System.Web.Http;
using Spark.Engine.Extensions;
using Spark.Engine.Core;

namespace Spark.Filters
{
    public class FhirExceptionFilter : ExceptionFilterAttribute
    {
       
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
            HttpResponseMessage response;

            if (context.Exception is SparkException)
            {
                var e = (SparkException)context.Exception;
                var outcome = e.Outcome == null ? CreateOutcome(e) : e.Outcome;
                response = context.Request.CreateResponse(e.StatusCode, outcome);
            }
            else if (context.Exception is HttpResponseException)
            {
                var e = (HttpResponseException)context.Exception;
                var outcome = new OperationOutcome().AddError(e.Response.ReasonPhrase);
                response = context.Request.CreateResponse(e.Response.StatusCode, outcome);
            }
            else
            {
                response = context.Request.CreateResponse(HttpStatusCode.InternalServerError, CreateOutcome(context.Exception));
            }

            throw new HttpResponseException(response);
        }
    }
}