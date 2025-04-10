/* 
 * Copyright (c) 2019-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Primitives;
using Spark.Engine.Core;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Spark.Engine.Formatters;

public class BinaryInputFormatter : InputFormatter
{
    public BinaryInputFormatter()
    {
        SupportedMediaTypes.Add(FhirMediaType.OctetStreamMimeType);
    }

    protected override bool CanReadType(Type type)
    {
        return type == typeof(Resource);
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue("X-Content-Type", out StringValues contentTypeHeaderValues))
            throw Error.BadRequest("Binary POST and PUT must provide a Content-Type header.");

        string contentType = contentTypeHeaderValues.FirstOrDefault();
        MemoryStream memoryStream = new MemoryStream();
        await context.HttpContext.Request.Body.CopyToAsync(memoryStream);
        Binary binary = new Binary
        {
            ContentType = contentType,
            Data = memoryStream.ToArray()
        };

        return await InputFormatterResult.SuccessAsync(binary);
    }
}
