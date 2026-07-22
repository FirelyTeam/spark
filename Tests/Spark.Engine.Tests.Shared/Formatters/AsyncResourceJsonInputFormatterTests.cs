/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using FhirModel = Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Spark.Engine.Core;
using Spark.Engine.Formatters;
using Spark.Engine.Tests.Utility;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Spark.Engine.Tests.Formatters;

public class AsyncResourceJsonInputFormatterTests : FormatterTestBase
{
    private const string DEFAULT_CONTENT_TYPE = "application/json";

    [Fact]
    public async Task ReadAsync_ValidResource_ReturnsResource()
    {
        var formatter = GetInputFormatter();

        var fhirVersionMoniker = FhirVersionUtility.GetFhirVersionMoniker();
        var content = GetResourceFromFileAsString(Path.Combine("TestData", fhirVersionMoniker.ToString(), "patient-example.json"));
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = GetHttpContext(contentBytes, DEFAULT_CONTENT_TYPE);

        var formatterContext = CreateInputFormatterContext(typeof(FhirModel.Resource), httpContext);

        var result = await formatter.ReadAsync(formatterContext);

        Assert.False(result.HasError);

        var patient = Assert.IsType<FhirModel.Patient>(result.Model);
        Assert.Equal("example", patient.Id);
    }

    [Fact]
    public async Task ReadAsync_ThrowsSparkException_BadRequest_OnMalformedBody()
    {
        var formatter = GetInputFormatter();

        var contentBytes = Encoding.UTF8.GetBytes("this is not json");
        var httpContext = GetHttpContext(contentBytes, DEFAULT_CONTENT_TYPE);

        var formatterContext = CreateInputFormatterContext(typeof(FhirModel.Resource), httpContext);

        SparkException exception = await Assert.ThrowsAsync<SparkException>(() => formatter.ReadAsync(formatterContext));
        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
    }

    private static AsyncResourceJsonInputFormatter GetInputFormatter(DeserializerSettings parserSettings = null)
    {
        if (parserSettings == null) parserSettings = new DeserializerSettings().UsingMode(DeserializationMode.Strict);
        return new AsyncResourceJsonInputFormatter(
            new FhirJsonDeserializer(parserSettings));
    }
}
