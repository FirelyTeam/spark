#if NETSTANDARD2_0
using FhirModel = Hl7.Fhir.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;

namespace Spark.Engine.ExceptionHandling
{
    // https://stackoverflow.com/a/38935583
    public class ErrorHandler
    {
        private readonly RequestDelegate _next;

        public ErrorHandler(RequestDelegate next)
        {
            _next = next;
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
                outcome = GetOperationOutcome(exception);

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