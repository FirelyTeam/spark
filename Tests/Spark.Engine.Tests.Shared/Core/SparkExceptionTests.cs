/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Engine.Core;
using System.Net;
using Xunit;

namespace Spark.Engine.Tests.Core;

public class SparkExceptionTests
{
    [Fact]
    public void Message_Without_Arguments_Is_Not_Formatted()
    {
        var exception = Error.BadRequest("Body parsing failed: '}' is an invalid start of a value.");

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal("Body parsing failed: '}' is an invalid start of a value.", exception.Message);
    }

    [Fact]
    public void Message_With_Arguments_Is_Formatted()
    {
        var exception = Error.NotFound("No {0} resource with id {1}", "Patient", "example");

        Assert.Equal("No Patient resource with id example", exception.Message);
    }
}
