/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Hl7.Fhir.Model;
using Spark.Engine.Extensions;
using Xunit;

namespace Spark.Engine.Tests.Extensions;

public class HttpRequestFhirExtensionsTests
{
    [Theory]
    [InlineData("W/\"1\"", "1")]
    [InlineData("W/\"123\"", "123")]
    [InlineData("\"1\"", "1")]
    [InlineData("\"123\"", "123")]
    public void IfMatchVersionId_ShouldExtractVersionFromETag(string headerValue, string expectedVersionId)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.IfMatch = new StringValues(headerValue);

        string actualVersionId = context.Request.IfMatchVersionId();

        Assert.Equal(expectedVersionId, actualVersionId);
    }

    [Fact]
    public void IfMatchVersionId_WithNoHeader_ShouldReturnNull()
    {
        var context = new DefaultHttpContext();

        string actualVersionId = context.Request.IfMatchVersionId();

        Assert.Null(actualVersionId);
    }

    [Fact]
    public void IfMatchVersionId_WithEmptyHeaders_ShouldReturnNull()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.IfMatch = "";

        string actualVersionId = context.Request.IfMatchVersionId();

        Assert.Null(actualVersionId);
    }

    [Theory]
    [InlineData("application/fhir+json;charset=utf-8")]
    [InlineData("application/fhir+xml; charset=utf-8")]
    public void IsContentTypeHeaderFhirMediaType_WithParameters_ShouldReturnTrue(string contentType)
    {
        bool isFhirMediaType = HttpRequestExtensions.IsContentTypeHeaderFhirMediaType(contentType);

        Assert.True(isFhirMediaType);
    }

    [Fact]
    public void TransferResourceIdIfRawBinary_WithFhirJsonAndCharset_ShouldKeepResourceIdNull()
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/fhir+json;charset=utf-8";
        var binary = new Binary();

        context.Request.TransferResourceIdIfRawBinary(binary, "example");

        Assert.Null(binary.Id);
    }

    [Fact]
    public void TransferResourceIdIfRawBinary_WithRawBinaryContentType_ShouldTransferResourceId()
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/pdf";
        var binary = new Binary();

        context.Request.TransferResourceIdIfRawBinary(binary, "example");

        Assert.Equal("example", binary.Id);
    }
}
