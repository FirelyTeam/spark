/*
 * Copyright (c) 2019-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using FhirModel = Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Engine.Formatters;

public class ResourceJsonOutputFormatter : TextOutputFormatter
{
    private readonly BaseFhirJsonSerializer _serializer;

    public ResourceJsonOutputFormatter(BaseFhirJsonSerializer serializer)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
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

    public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(selectedEncoding);
        if (selectedEncoding != Encoding.UTF8) throw Error.BadRequest($"FHIR supports UTF-8 encoding exclusively, not {selectedEncoding.WebName}");

        byte[] writeBuffer = [];
        SummaryType summaryType = context.HttpContext.Request.RequestSummary();
        if (context.Object is FhirResponse response)
        {
            context.HttpContext.Response.AcquireHeaders(response);
            context.HttpContext.Response.StatusCode = (int)response.StatusCode;

            if (response.Resource != null)
                writeBuffer = _serializer.SerializeToBytes(response.Resource, summaryType);
        }
        else if (context.Object is FhirModel.Resource resource)
        {
            writeBuffer = _serializer.SerializeToBytes(resource, summaryType);
        }
        else if (context.Object is ValidationProblemDetails validationProblems)
        {
            FhirModel.OperationOutcome outcome = new();
            outcome.AddValidationProblems(context.HttpContext.GetResourceType(), (HttpStatusCode)context.HttpContext.Response.StatusCode, validationProblems);
            writeBuffer = _serializer.SerializeToBytes(outcome, summaryType);
        }

        return writeBuffer.Length > 0
            ? context.HttpContext.Response.Body.WriteAsync(writeBuffer).AsTask()
            : Task.CompletedTask;
    }
}
