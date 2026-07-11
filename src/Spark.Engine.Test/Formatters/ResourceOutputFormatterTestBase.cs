/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Spark.Engine.Formatters;
using System;
using System.IO;
using System.Text;
using System.Xml;
using Tasks = System.Threading.Tasks;
using FhirModel = Hl7.Fhir.Model;

namespace Spark.Engine.Test.Formatters;

public abstract class ResourceOutputFormatterTestBase
{
    public enum OutputFormatterKind
    {
        Json,
        AsyncJson,
        Xml,
        AsyncXml
    }

    protected static TextOutputFormatter CreateFormatter(OutputFormatterKind formatterKind)
    {
        return formatterKind switch
        {
            OutputFormatterKind.Json => new ResourceJsonOutputFormatter(),
            OutputFormatterKind.AsyncJson => new AsyncResourceJsonOutputFormatter(),
            OutputFormatterKind.Xml => new ResourceXmlOutputFormatter(),
            OutputFormatterKind.AsyncXml => new AsyncResourceXmlOutputFormatter(),
            _ => throw new ArgumentOutOfRangeException(nameof(formatterKind), formatterKind, null)
        };
    }

    protected static HttpContext CreateHttpContext(string queryString)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new NonDisposingMemoryStream();
        httpContext.Request.QueryString = new QueryString(queryString);
        httpContext.RequestServices = new FormatterServiceProvider();
        return httpContext;
    }

    protected static OutputFormatterWriteContext CreateOutputFormatterContext(HttpContext httpContext, FhirModel.Resource resource)
    {
        return CreateOutputFormatterContext(httpContext, resource, typeof(FhirModel.Resource));
    }

    protected static OutputFormatterWriteContext CreateOutputFormatterContext(HttpContext httpContext, object outputObject, Type objectType)
    {
        return new OutputFormatterWriteContext(
            httpContext,
            static (stream, encoding) => new StreamWriter(stream, encoding),
            objectType,
            outputObject);
    }

    protected static async Tasks.Task WriteResponseBodyAsync(TextOutputFormatter formatter, OutputFormatterWriteContext formatterContext)
    {
        await formatter.WriteResponseBodyAsync(formatterContext, Encoding.UTF8);
    }

    protected static string ReadResponseBody(HttpContext httpContext)
    {
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    protected static string CreateExpectedOutput(OutputFormatterKind formatterKind, FhirModel.Resource resource, SummaryType summaryType)
    {
        return formatterKind switch
        {
            OutputFormatterKind.Json or OutputFormatterKind.AsyncJson =>
                new FhirJsonSerializer().SerializeToString(resource, summaryType),
            OutputFormatterKind.Xml =>
                CreateExpectedXmlOutput(resource, summaryType),
            OutputFormatterKind.AsyncXml =>
                new FhirXmlSerializer().SerializeToString(resource, summaryType),
            _ => throw new ArgumentOutOfRangeException(nameof(formatterKind), formatterKind, null)
        };
    }

    private static string CreateExpectedXmlOutput(FhirModel.Resource resource, SummaryType summaryType)
    {
        using var stream = new MemoryStream();
        using (var writer = new XmlTextWriter(stream, new UTF8Encoding(false)))
        {
            new FhirXmlSerializer().Serialize(resource, writer, summaryType);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private sealed class FormatterServiceProvider : IServiceProvider
    {
        private readonly FhirJsonSerializer _jsonSerializer = new();
        private readonly FhirXmlSerializer _xmlSerializer = new();

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(FhirJsonSerializer))
                return _jsonSerializer;

            if (serviceType == typeof(FhirXmlSerializer))
                return _xmlSerializer;

            return null;
        }
    }

    private sealed class NonDisposingMemoryStream : MemoryStream
    {
        protected override void Dispose(bool disposing)
        {
        }
    }
}
