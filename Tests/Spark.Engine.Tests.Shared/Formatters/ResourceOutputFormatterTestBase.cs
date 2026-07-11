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
using System.Threading.Tasks;
using FhirModel = Hl7.Fhir.Model;

namespace Spark.Engine.Tests.Formatters;

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
        var modelInspector = new Spark.Engine.Core.FhirModel().GetModelInspector();

        return formatterKind switch
        {
            OutputFormatterKind.Json => new ResourceJsonOutputFormatter(new BaseFhirJsonSerializer(modelInspector)),
            OutputFormatterKind.AsyncJson => new AsyncResourceJsonOutputFormatter(new BaseFhirJsonSerializer(modelInspector)),
            OutputFormatterKind.Xml => new ResourceXmlOutputFormatter(new BaseFhirXmlSerializer(modelInspector)),
            OutputFormatterKind.AsyncXml => new AsyncResourceXmlOutputFormatter(new BaseFhirXmlSerializer(modelInspector)),
            _ => throw new ArgumentOutOfRangeException(nameof(formatterKind), formatterKind, null)
        };
    }

    protected static HttpContext CreateHttpContext(string queryString)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.Request.QueryString = new QueryString(queryString);
        return httpContext;
    }

    protected static OutputFormatterWriteContext CreateOutputFormatterContext(HttpContext httpContext, FhirModel.Patient patient)
    {
        return new OutputFormatterWriteContext(
            httpContext,
            static (stream, encoding) => new StreamWriter(stream, encoding),
            typeof(FhirModel.Resource),
            patient);
    }

    protected static async Task WriteResponseBodyAsync(TextOutputFormatter formatter, OutputFormatterWriteContext formatterContext)
    {
        await formatter.WriteResponseBodyAsync(formatterContext, Encoding.UTF8);
    }

    protected static string ReadResponseBody(HttpContext httpContext)
    {
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    protected static string CreateExpectedOutput(OutputFormatterKind formatterKind, FhirModel.Patient patient, SummaryType summaryType)
    {
        var modelInspector = new Spark.Engine.Core.FhirModel().GetModelInspector();
        byte[] bytes = formatterKind switch
        {
            OutputFormatterKind.Json or OutputFormatterKind.AsyncJson =>
                new BaseFhirJsonSerializer(modelInspector).SerializeToBytes(patient, summaryType),
            OutputFormatterKind.Xml or OutputFormatterKind.AsyncXml =>
                new BaseFhirXmlSerializer(modelInspector).SerializeToBytes(patient, summaryType),
            _ => throw new ArgumentOutOfRangeException(nameof(formatterKind), formatterKind, null)
        };

        return Encoding.UTF8.GetString(bytes);
    }
}
