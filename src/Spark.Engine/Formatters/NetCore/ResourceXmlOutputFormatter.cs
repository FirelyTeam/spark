/* 
 * Copyright (c) 2019-2024, Incendi (info@incendi.no)
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

#if NETSTANDARD2_0 || NET6_0
using FhirModel = Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Spark.Engine.Formatters
{
    public class ResourceXmlOutputFormatter : TextOutputFormatter
    {
        public ResourceXmlOutputFormatter()
        {
            SupportedEncodings.Clear();
            SupportedEncodings.Add(Encoding.UTF8);

            foreach (var mediaType in FhirMediaType.XmlMimeTypes)
            {
                SupportedMediaTypes.Add(mediaType);
            }
        }

        protected override bool CanWriteType(Type type)
        {
            return 
                typeof(FhirModel.Resource).IsAssignableFrom(type) 
                || typeof(FhirResponse).IsAssignableFrom(type)
                || typeof(ValidationProblemDetails).IsAssignableFrom(type);
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (selectedEncoding == null) throw new ArgumentNullException(nameof(selectedEncoding));
            if (selectedEncoding != Encoding.UTF8) throw Error.BadRequest($"FHIR supports UTF-8 encoding exclusively, not {selectedEncoding.WebName}");

            context.HttpContext.AllowSynchronousIO();

            using (TextWriter writer = context.WriterFactory(context.HttpContext.Response.Body, selectedEncoding))
            using (XmlWriter xmlWriter = new XmlTextWriter(writer))
            {
                if (!(context.HttpContext.RequestServices.GetService(typeof(FhirXmlSerializer)) is FhirXmlSerializer serializer))
                    throw Error.Internal($"Missing required dependency '{nameof(FhirXmlSerializer)}'");

                SummaryType summaryType = context.HttpContext.Request.RequestSummary();
                if (typeof(FhirResponse).IsAssignableFrom(context.ObjectType))
                {
                    FhirResponse response = context.Object as FhirResponse;

                    context.HttpContext.Response.AcquireHeaders(response);
                    context.HttpContext.Response.StatusCode = (int)response.StatusCode;

                    if (response.Resource != null)
                        serializer.Serialize(response.Resource, xmlWriter, summaryType);
                }
                else if (context.ObjectType == typeof(FhirModel.OperationOutcome) || typeof(FhirModel.Resource).IsAssignableFrom(context.ObjectType))
                {
                    if (context.Object != null)
                        serializer.Serialize(context.Object as FhirModel.Resource, xmlWriter, summaryType);
                }
                else if(context.Object is ValidationProblemDetails validationProblems)
                {
                    FhirModel.OperationOutcome outcome = new FhirModel.OperationOutcome();
                    outcome.AddValidationProblems(context.HttpContext.GetResourceType(), (HttpStatusCode)context.HttpContext.Response.StatusCode, validationProblems);
                    serializer.Serialize(outcome, xmlWriter, summaryType);
                }
            }

            return Task.CompletedTask;
        }
    }
}
#endif