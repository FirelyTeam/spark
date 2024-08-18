/* 
 * Copyright (c) 2016-2018, Firely (info@fire.ly)
 * Copyright (c) 2019-2024, Incendi (info@incendi.no)
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

#if NET462
using System;
using System.Net;
using System.Net.Http;
using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using System.Web.Http;


namespace Spark.Engine.ExceptionHandling
{
    public class ExceptionResponseMessageFactory : IExceptionResponseMessageFactory
    {
        public HttpResponseMessage GetResponseMessage(Exception exception, HttpRequestMessage request)
        {
            if (exception == null)
                return null;
            HttpResponseMessage response = InternalCreateHttpResponseMessage(exception as SparkException, request) ??
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
#endif