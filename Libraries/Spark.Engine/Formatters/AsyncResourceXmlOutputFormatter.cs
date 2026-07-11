/*
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using FhirModel = Hl7.Fhir.Model;
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

public class AsyncResourceXmlOutputFormatter : TextOutputFormatter
{
    private readonly BaseFhirXmlSerializer _serializer;

    public AsyncResourceXmlOutputFormatter(BaseFhirXmlSerializer serializer)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
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

    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(selectedEncoding);
        if (!Equals(selectedEncoding, Encoding.UTF8)) throw Error.BadRequest($"FHIR supports UTF-8 encoding exclusively, not {selectedEncoding.WebName}");

        var responseBody = context.HttpContext.Response.Body;
        byte[] writeBuffer = [];
        var summaryType = context.HttpContext.Request.GetSummaryType();
        bool returnPrettyFormatted = context.HttpContext.Request.ReturnPrettyFormattedOutput();

        if (context.Object is FhirResponse response)
        {
            context.HttpContext.Response.AcquireHeaders(response);
            context.HttpContext.Response.StatusCode = (int)response.StatusCode;

            if (response.Resource != null)
            {
                writeBuffer = _serializer.SerializeToBytes(response.Resource, summaryType, pretty: returnPrettyFormatted);
            }
        }
        else if (context.ObjectType == typeof(FhirModel.OperationOutcome) || typeof(FhirModel.Resource).IsAssignableFrom(context.ObjectType))
        {
            if (context.Object is FhirModel.Resource resource)
            {
                writeBuffer = _serializer.SerializeToBytes(resource, summaryType, pretty: returnPrettyFormatted);
            }
        }
        else if (context.Object is ValidationProblemDetails validationProblems)
        {
            FhirModel.OperationOutcome outcome = new();
            outcome.AddValidationProblems(context.HttpContext.GetResourceType(), (HttpStatusCode)context.HttpContext.Response.StatusCode, validationProblems);
            writeBuffer = _serializer.SerializeToBytes(outcome, summaryType, pretty: returnPrettyFormatted);
        }

        if (writeBuffer.Length > 0)
        {
            await responseBody.WriteAsync(writeBuffer);
            await responseBody.FlushAsync();
        }
    }
}
