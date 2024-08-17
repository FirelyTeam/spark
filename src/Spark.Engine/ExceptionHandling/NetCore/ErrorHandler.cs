/*
 * Copyright (c) 2019-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

#if NETSTANDARD2_0 || NET6_0
using FhirModel = Hl7.Fhir.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Extensions.Logging;

namespace Spark.Engine.ExceptionHandling
{
    // https://stackoverflow.com/a/38935583
    public class ErrorHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandler> _logger;

        public ErrorHandler(RequestDelegate next, ILogger<ErrorHandler> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                await HandleExceptionAsync(context, exception);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            HttpStatusCode code = HttpStatusCode.InternalServerError;
            FhirModel.OperationOutcome outcome;
            if (exception is SparkException ex1)
            {
                code = ex1.StatusCode;
                outcome = GetOperationOutcome(ex1);
            }
            else if (exception is HttpResponseException ex2)
            {
                code = ex2.Response.StatusCode;
                outcome = GetOperationOutcome(ex2);
            }
            else
            {
                _logger.LogError(exception, exception.Message);
                outcome = GetOperationOutcome(exception);
            }

            // Set HTTP status code 
            context.Response.StatusCode = (int)code;
            OutputFormatterWriteContext writeContext = context.GetOutputFormatterWriteContext(outcome);
            IOutputFormatter formatter = context.SelectFormatter(writeContext);
            // Write the OperationOutcome to the Response using an OutputFormatter from the request pipeline
            await formatter.WriteAsync(writeContext);
        }

        private FhirModel.OperationOutcome GetOperationOutcome(SparkException exception)
        {
            if (exception == null) return null;
            return (exception.Outcome ?? new FhirModel.OperationOutcome()).AddAllInnerErrors(exception);
        }

        private FhirModel.OperationOutcome GetOperationOutcome(HttpResponseException exception)
        {
            if (exception == null) return null;
            return new FhirModel.OperationOutcome().AddError(exception.Response.ReasonPhrase);
        }

        private FhirModel.OperationOutcome GetOperationOutcome(Exception exception)
        {
            if (exception == null) return null;
            return new FhirModel.OperationOutcome().AddAllInnerErrors(exception);
        }
    }
}
#endif