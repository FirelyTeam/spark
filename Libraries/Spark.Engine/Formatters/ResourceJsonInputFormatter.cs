/* 
 * Copyright (c) 2019-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.WebUtilities;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Spark.Engine.Formatters;

public class ResourceJsonInputFormatter : TextInputFormatter
{
    private readonly BaseFhirJsonDeserializer _deserializer;

    public ResourceJsonInputFormatter(BaseFhirJsonDeserializer deserializer, ArrayPool<char> charPool)
    {
        ArgumentNullException.ThrowIfNull(deserializer);
        ArgumentNullException.ThrowIfNull(charPool);

        _deserializer = deserializer;

        SupportedEncodings.Clear();
        SupportedEncodings.Add(Encoding.UTF8);

        foreach (var mediaType in FhirMediaType.JsonMimeTypes)
        {
            SupportedMediaTypes.Add(mediaType);
        }
    }

    protected override bool CanReadType(Type type)
    {
        return typeof(Resource).IsAssignableFrom(type);
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(encoding);
        if (!Equals(encoding, Encoding.UTF8))
            throw Error.BadRequest("FHIR supports UTF-8 encoding exclusively, not " + encoding.WebName);

        context.HttpContext.AllowSynchronousIO();

        var request = context.HttpContext.Request;
        if (!request.Body.CanSeek)
        {
            request.EnableBuffering();
            Debug.Assert(request.Body.CanSeek);

            await request.Body.DrainAsync(context.HttpContext.RequestAborted);
            request.Body.Seek(0L, SeekOrigin.Begin);
        }

        try
        {
            using TextReader streamReader = context.ReaderFactory(request.Body, encoding);
            var body = await streamReader.ReadToEndAsync();
            var resource = _deserializer.Deserialize<Resource>(body);
            context.HttpContext.AddResourceType(resource.GetType());

            return await InputFormatterResult.SuccessAsync(resource);
        }
        catch (FormatException exception)
        {
            throw Error.BadRequest($"Body parsing failed: {exception.Message}");
        }
        catch (JsonException exception)
        {
            throw Error.BadRequest($"Body parsing failed: {exception.Message}");
        }
    }
}
