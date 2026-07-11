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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FhirModel = Hl7.Fhir.Model;

namespace Spark.Engine.Tests.Formatters;

public class ResourceOutputFormatterSummaryTests
{
    public enum OutputFormatterKind
    {
        Json,
        AsyncJson,
        Xml,
        AsyncXml
    }

    public static IEnumerable<object[]> FormatterSummaryCases()
    {
        foreach (OutputFormatterKind formatterKind in Enum.GetValues<OutputFormatterKind>())
        {
            yield return [formatterKind, "?_summary=true", SummaryType.True];
            yield return [formatterKind, "?_summary=text", SummaryType.Text];
            yield return [formatterKind, "?_summary=data", SummaryType.Data];
            yield return [formatterKind, "?_summary=false", SummaryType.False];
            yield return [formatterKind, string.Empty, SummaryType.False];
        }
    }

    [Theory]
    [MemberData(nameof(FormatterSummaryCases))]
    public async Task WriteResponseBodyAsync_UsesSummaryParameter(
        OutputFormatterKind formatterKind,
        string queryString,
        SummaryType expectedSummaryType)
    {
        var patient = CreatePatient();
        var httpContext = CreateHttpContext(queryString);
        var formatterContext = CreateOutputFormatterContext(httpContext, patient);
        var formatter = CreateFormatter(formatterKind);

        await WriteResponseBodyAsync(formatter, formatterContext);

        var output = ReadResponseBody(httpContext);
        var expected = CreateExpectedOutput(formatterKind, patient, expectedSummaryType);

        Assert.Equal(expected, output);
    }

    private static TextOutputFormatter CreateFormatter(OutputFormatterKind formatterKind)
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

    private static HttpContext CreateHttpContext(string queryString)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.Request.QueryString = new QueryString(queryString);
        return httpContext;
    }

    private static FhirModel.Patient CreatePatient()
    {
        return new FhirModel.Patient
        {
            Id = "example",
            Meta = new FhirModel.Meta
            {
                VersionId = "1",
                LastUpdated = DateTimeOffset.Parse("2026-01-01T00:00:00+00:00")
            },
            Text = new FhirModel.Narrative
            {
                Status = FhirModel.Narrative.NarrativeStatus.Generated,
                Div = "<div xmlns=\"http://www.w3.org/1999/xhtml\">Example patient</div>"
            },
            Active = true,
            Name =
            [
                new FhirModel.HumanName
                {
                    Family = "Smith",
                    Given = ["John"]
                }
            ],
            Gender = FhirModel.AdministrativeGender.Male,
            BirthDate = "1974-12-25"
        };
    }

    private static OutputFormatterWriteContext CreateOutputFormatterContext(HttpContext httpContext, FhirModel.Patient patient)
    {
        return new OutputFormatterWriteContext(
            httpContext,
            static (stream, encoding) => new StreamWriter(stream, encoding),
            typeof(FhirModel.Resource),
            patient);
    }

    private static async Task WriteResponseBodyAsync(TextOutputFormatter formatter, OutputFormatterWriteContext formatterContext)
    {
        await formatter.WriteResponseBodyAsync(formatterContext, Encoding.UTF8);
    }

    private static string ReadResponseBody(HttpContext httpContext)
    {
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static string CreateExpectedOutput(OutputFormatterKind formatterKind, FhirModel.Patient patient, SummaryType summaryType)
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
