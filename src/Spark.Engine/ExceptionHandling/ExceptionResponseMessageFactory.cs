using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Engine.Extensions;

namespace Spark.Engine.ExceptionHandling
{
    public class ExceptionResponseMessageFactory : IExceptionResponseMessageFactory
    {
        private SparkException ex;

        public HttpResponseMessage GetResponseMessage(Exception exception, HttpRequestMessage request)
        {
            if (exception == null)
                return null;

            HttpResponseMessage response = null;
            response = InternalCreateHttpResponseMessage(exception as SparkException, request) ??
                InternalCreateHttpResponseMessage(exception as HttpResponseException, request) ??
                InternalCreateHttpResponseMessage(exception, request);

            return response;
        }

        private HttpResponseMessage InternalCreateHttpResponseMessage(SparkException exception, HttpRequestMessage request)
        {
            if (exception == null)
                return null;

            OperationOutcome outcome = exception.Outcome ?? new OperationOutcome();
            outcome.AddAllInnerErrors(exception);
            return request.CreateResponse(exception.StatusCode, outcome); ;
        }

        private HttpResponseMessage InternalCreateHttpResponseMessage(HttpResponseException exception, HttpRequestMessage request)
        {
            if (exception == null)
                return null;

            OperationOutcome outcome =  new OperationOutcome().AddError(exception.Response.ReasonPhrase);
            return request.CreateResponse(exception.Response.StatusCode, outcome);
        }

        private HttpResponseMessage InternalCreateHttpResponseMessage(Exception exception, HttpRequestMessage request)
        {
            if (exception == null)
                return null;

            OperationOutcome outcome = new OperationOutcome().AddAllInnerErrors(exception);
            return request.CreateResponse(HttpStatusCode.InternalServerError, outcome);
        }
    }
}