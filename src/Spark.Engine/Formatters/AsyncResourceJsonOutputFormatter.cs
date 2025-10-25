/* 
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using FhirModel = Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc.Formatters;
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
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(selectedEncoding);
            if (!Equals(selectedEncoding, Encoding.UTF8)) throw Error.BadRequest($"FHIR supports UTF-8 encoding exclusively, not {selectedEncoding.WebName}");

            if (!(context.HttpContext.RequestServices.GetService(typeof(FhirJsonSerializer)) is FhirJsonSerializer serializer))
                throw Error.Internal($"Missing required dependency '{nameof(FhirJsonSerializer)}'");

            var responseBody = context.HttpContext.Response.Body;
            byte[] writeBuffer = [];
            var summaryType = context.HttpContext.Request.RequestSummary();

            if (context.Object is FhirResponse response)
            {
                context.HttpContext.Response.AcquireHeaders(response);
                context.HttpContext.Response.StatusCode = (int)response.StatusCode;

                if (response.Resource != null)
                {
                    writeBuffer = await serializer.SerializeToBytesAsync(response.Resource, summaryType);
                }
            }
            else if (context.ObjectType == typeof(FhirModel.OperationOutcome) || typeof(FhirModel.Resource).IsAssignableFrom(context.ObjectType))
            {
                if (context.Object is FhirModel.Resource resource)
                {
                    writeBuffer = await serializer.SerializeToBytesAsync(resource, summaryType);
                }
            }
            else if (context.Object is ValidationProblemDetails validationProblems)
            {
                FhirModel.OperationOutcome outcome = new();
                outcome.AddValidationProblems(context.HttpContext.GetResourceType(), (HttpStatusCode)context.HttpContext.Response.StatusCode, validationProblems);
                writeBuffer = await serializer.SerializeToBytesAsync(outcome, summaryType);
            }

            await responseBody.WriteAsync(writeBuffer);
            await responseBody.FlushAsync();
        }
    }
}
