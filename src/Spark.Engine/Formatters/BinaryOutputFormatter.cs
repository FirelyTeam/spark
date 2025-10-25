/* 
 * Copyright (c) 2019-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.AspNetCore.Http;
using FhirModel = Hl7.Fhir.Model;
using Microsoft.AspNetCore.Mvc.Formatters;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using System;
using System.Threading.Tasks;

namespace Spark.Engine.Formatters;

public class BinaryOutputFormatter : OutputFormatter
{
    public BinaryOutputFormatter()
    {
        SupportedMediaTypes.Add(FhirMediaType.OctetStreamMimeType);
    }

    protected override bool CanWriteType(Type type)
    {
        return typeof(FhirModel.Binary).IsAssignableFrom(type) || typeof(FhirResponse).IsAssignableFrom(type);
    }

    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
    {
        if (typeof(FhirModel.Binary).IsAssignableFrom(context.ObjectType) || typeof(FhirResponse).IsAssignableFrom(context.ObjectType))
        {
            FhirModel.Binary binary = null;
            if (typeof(FhirResponse).IsAssignableFrom(context.ObjectType))
            {
                FhirResponse response = (FhirResponse)context.Object;

                context.HttpContext.Response.AcquireHeaders(response);
                context.HttpContext.Response.StatusCode = (int)response.StatusCode;

                binary = response.Resource as FhirModel.Binary;
            }
            if (binary == null) return;

            context.HttpContext.Response.Headers.Append(HttpHeaderName.CONTENT_DISPOSITION, "attachment");
            context.HttpContext.Response.ContentType = binary.ContentType;

            var responseBody = context.HttpContext.Response.Body;
            byte[] writeBuffer = binary.Data;
            await responseBody.WriteAsync(writeBuffer);
            await responseBody.FlushAsync();
        }
    }
}
