/* 
 * Copyright (c) 2021-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

#if NETSTANDARD2_0 || NET6_0
using FhirModel = Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Spark.Engine.Formatters
{
    public class AsyncResourceJsonOutputFormatter : TextOutputFormatter
    {
        public AsyncResourceJsonOutputFormatter()
        {
            SupportedEncodings.Clear();
            SupportedEncodings.Add(Encoding.UTF8);

            foreach (var mediaType in FhirMediaType.JsonMimeTypes)
            {
                SupportedMediaTypes.Add(mediaType);
            }
        }

        protected override bool CanWriteType(Type type)
        {
            return typeof(FhirModel.Resource).IsAssignableFrom(type) 
                || typeof(FhirResponse).IsAssignableFrom(type)
                || typeof(ValidationProblemDetails).IsAssignableFrom(type);
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (selectedEncoding == null) throw new ArgumentNullException(nameof(selectedEncoding));
            if (selectedEncoding != Encoding.UTF8) throw Error.BadRequest($"FHIR supports UTF-8 encoding exclusively, not {selectedEncoding.WebName}");

            if (!(context.HttpContext.RequestServices.GetService(typeof(FhirJsonSerializer)) is FhirJsonSerializer serializer))
                throw Error.Internal($"Missing required dependency '{nameof(FhirJsonSerializer)}'");

            var responseBody = context.HttpContext.Response.Body;
            var writeBodyString = string.Empty;
            var summaryType = context.HttpContext.Request.RequestSummary();

            if (typeof(FhirResponse).IsAssignableFrom(context.ObjectType))
            {
                FhirResponse response = context.Object as FhirResponse;

                context.HttpContext.Response.AcquireHeaders(response);
                context.HttpContext.Response.StatusCode = (int)response.StatusCode;

                if (response.Resource != null)
                {
                    writeBodyString = serializer.SerializeToString(response.Resource, summaryType);
                }
            }
            else if (context.ObjectType == typeof(FhirModel.OperationOutcome) || typeof(FhirModel.Resource).IsAssignableFrom(context.ObjectType))
            {
                if (context.Object != null)
                {
                    writeBodyString = serializer.SerializeToString(context.Object as FhirModel.Resource, summaryType);
                }
            }
            else if (context.Object is ValidationProblemDetails validationProblems)
            {
                FhirModel.OperationOutcome outcome = new FhirModel.OperationOutcome();
                outcome.AddValidationProblems(context.HttpContext.GetResourceType(), (HttpStatusCode)context.HttpContext.Response.StatusCode, validationProblems);
                writeBodyString = serializer.SerializeToString(outcome, summaryType);
            }

            if (!string.IsNullOrWhiteSpace(writeBodyString))
            {
                var writeBuffer = selectedEncoding.GetBytes(writeBodyString);
                await responseBody.WriteAsync(writeBuffer, 0, writeBuffer.Length);
                await responseBody.FlushAsync();
            }
        }
    }
}
#endif