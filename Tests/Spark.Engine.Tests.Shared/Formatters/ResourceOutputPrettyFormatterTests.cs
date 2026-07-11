/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using FhirModel = Hl7.Fhir.Model;

namespace Spark.Engine.Tests.Formatters;

public class ResourceOutputPrettyFormatterTests : ResourceOutputFormatterTestBase
{
    public enum OutputObjectKind
    {
        FhirResponse,
        Resource,
        ValidationProblemDetails
    }

    public static IEnumerable<object[]> FormatterPrettyCases()
    {
        foreach (OutputFormatterKind formatterKind in Enum.GetValues<OutputFormatterKind>())
        {
            foreach (OutputObjectKind objectKind in Enum.GetValues<OutputObjectKind>())
            {
                yield return [formatterKind, objectKind, "?_pretty=true", true];
                yield return [formatterKind, objectKind, "?_pretty=false", false];
                yield return [formatterKind, objectKind, string.Empty, false];
            }
        }
    }

    [Theory]
    [MemberData(nameof(FormatterPrettyCases))]
    public async Task WriteResponseBodyAsync_ReturnsExpectedFormatting(
        OutputFormatterKind formatterKind,
        OutputObjectKind objectKind,
        string queryString,
        bool expectedPretty)
    {
        var httpContext = CreateHttpContext(queryString, objectKind);
        var outputObject = CreateOutputObject(objectKind);
        var formatterContext = CreateOutputFormatterContext(httpContext, outputObject, GetOutputObjectType(objectKind));
        var formatter = CreateFormatter(formatterKind);

        await WriteResponseBodyAsync(formatter, formatterContext);

        var output = ReadResponseBody(httpContext);

        Assert.Equal(expectedPretty, IsPrettyFormatted(output));
        AssertExpectedContent(output, objectKind);

        if (objectKind == OutputObjectKind.FhirResponse)
        {
            Assert.Equal((int)HttpStatusCode.Accepted, httpContext.Response.StatusCode);
        }
    }

    private static HttpContext CreateHttpContext(string queryString, OutputObjectKind objectKind)
    {
        var httpContext = CreateHttpContext(queryString);

        if (objectKind == OutputObjectKind.ValidationProblemDetails)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            httpContext.AddResourceType(typeof(FhirModel.Patient));
        }

        return httpContext;
    }

    private static object CreateOutputObject(OutputObjectKind objectKind)
    {
        return objectKind switch
        {
            OutputObjectKind.FhirResponse => new FhirResponse(HttpStatusCode.Accepted, CreatePatient()),
            OutputObjectKind.Resource => CreatePatient(),
            OutputObjectKind.ValidationProblemDetails => new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    ["active"] = ["The active field is required."]
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(objectKind), objectKind, null)
        };
    }

    private static Type GetOutputObjectType(OutputObjectKind objectKind)
    {
        return objectKind switch
        {
            OutputObjectKind.FhirResponse => typeof(FhirResponse),
            OutputObjectKind.Resource => typeof(FhirModel.Resource),
            OutputObjectKind.ValidationProblemDetails => typeof(ValidationProblemDetails),
            _ => throw new ArgumentOutOfRangeException(nameof(objectKind), objectKind, null)
        };
    }

    private static FhirModel.Patient CreatePatient()
    {
        return new FhirModel.Patient
        {
            Id = "example",
            Active = true
        };
    }

    private static OutputFormatterWriteContext CreateOutputFormatterContext(HttpContext httpContext, object outputObject, Type objectType)
    {
        return new OutputFormatterWriteContext(
            httpContext,
            static (stream, encoding) => new StreamWriter(stream, encoding),
            objectType,
            outputObject);
    }

    private static bool IsPrettyFormatted(string output)
    {
        return output.Contains('\n');
    }

    private static void AssertExpectedContent(string output, OutputObjectKind objectKind)
    {
        if (objectKind == OutputObjectKind.ValidationProblemDetails)
        {
            Assert.Contains("OperationOutcome", output);
            Assert.Contains("The active field is required.", output);
            return;
        }

        Assert.Contains("Patient", output);
        Assert.Contains("example", output);
    }
}
