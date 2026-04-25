/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
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
}
