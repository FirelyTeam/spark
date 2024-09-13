/* 
 * Copyright (c) 2020-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.IO;

namespace Spark.Engine.Test.Formatters;

public class FormatterTestBase
{
    protected string GetResourceFromFileAsString(string path)
    {
        using TextReader reader = new StreamReader(path);
        return reader.ReadToEnd();
    }

    protected static HttpContext GetHttpContext(
        byte[] contentBytes,
        string contentType)
    {
        return GetHttpContext(new MemoryStream(contentBytes), contentType);
    }

    protected static HttpContext GetHttpContext(Stream requestStream, string contentType)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = requestStream;
        httpContext.Request.ContentType = contentType;

        return httpContext;
    }

    protected static InputFormatterContext CreateInputFormatterContext(
        Type modelType,
        HttpContext httpContext,
        string modelName = null,
        bool treatEmptyInputAsDefaultValue = false)
    {
        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(modelType);

        return new InputFormatterContext(
            httpContext,
            modelName: modelName ?? string.Empty,
            modelState: new ModelStateDictionary(),
            metadata: metadata,
            readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader,
            treatEmptyInputAsDefaultValue: treatEmptyInputAsDefaultValue);
    }
}