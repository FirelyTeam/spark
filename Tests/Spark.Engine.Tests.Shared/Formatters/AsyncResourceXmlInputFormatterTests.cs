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
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Spark.Engine.Tests.Formatters;

public class AsyncResourceXmlInputFormatterTests : FormatterTestBase
{
    private const string DefaultContentType = "application/xml";

    [Theory]
    [InlineData("application/fhir+xml", true)]
    [InlineData("application/xml+fhir", true)]
    [InlineData("application/xml", true)]
    [InlineData("application/*", false)]
    [InlineData("*/*", false)]
    [InlineData("text/xml", true)]
    [InlineData("text/*", false)]
    [InlineData("text/json", false)]
    [InlineData("application/json", false)]
    [InlineData("application/some.entity+xml", true)]
    [InlineData("application/some.entity+xml;v=2", true)]
    [InlineData("application/some.entity+json", false)]
    [InlineData("application/some.entity+*", false)]
    [InlineData("text/some.entity+xml", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("invalid", false)]
    public void CanRead_ReturnsTrueForSupportedContent(string contentType, bool expectedCanRead)
    {
        var formatter = GetInputFormatter();

        var contentBytes = Encoding.UTF8.GetBytes("{ \"resourceType\": \"Patient\", \"id\": \"example\", \"active\": true }");
        var httpContext = GetHttpContext(contentBytes, contentType);

        var formatterContext = CreateInputFormatterContext(typeof(FhirModel.Resource), httpContext);

        var result = formatter.CanRead(formatterContext);

        Assert.Equal(expectedCanRead, result);
    }

    [Fact]
    public void SupportedMediaTypes_DefaultMediaType_ReturnsApplicationJson()
    {
        var formatter = GetInputFormatter();

        var mediaType = formatter.SupportedMediaTypes[0];

        Assert.Equal("application/fhir+xml", mediaType);
    }

    [Fact]
    public async Task ReadAsync_ValidResource_ReturnsResource()
    {
        var formatter = GetInputFormatter();

        var fhirVersionMoniker = FhirVersionUtility.GetFhirVersionMoniker();
        var content = GetResourceFromFileAsString(Path.Combine("TestData", fhirVersionMoniker.ToString(), "patient-example.xml"));
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = GetHttpContext(contentBytes, DefaultContentType);

        var formatterContext = CreateInputFormatterContext(typeof(FhirModel.Resource), httpContext);

        var result = await formatter.ReadAsync(formatterContext);

        Assert.False(result.HasError);

        var patient = Assert.IsType<FhirModel.Patient>(result.Model);
        Assert.Equal("example", patient.Id);
    }

    [Fact]
    public async Task ReadAsync_ThrowsSparkException_BadRequest_OnNonUtf8Content()
    {
        var formatter = GetInputFormatter();

        var contentBytes = "<Patient xmlns=\"http://hl7.org/fhir\"><id value=\"invalid-utf8\" /></Patient>"u8.ToArray();
        contentBytes["<Patient xmlns=\"http://hl7.org/fhir\"><id value=\"invalid-".Length] = 0xC3;
        contentBytes["<Patient xmlns=\"http://hl7.org/fhir\"><id value=\"invalid-".Length + 1] = 0x28;

        var httpContext = GetHttpContext(contentBytes, DefaultContentType);

        var formatterContext = CreateInputFormatterContext(typeof(FhirModel.Resource), httpContext);

        SparkException exception = await Assert.ThrowsAsync<SparkException>(() => formatter.ReadAsync(formatterContext));
        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
    }

    [Fact]
    public async Task ReadAsync_ThrowsSparkException_WithOperationOutcome_OnInvalidElement()
    {
        var formatter = GetInputFormatter();

        var contentBytes = "<Patient xmlns=\"http://hl7.org/fhir\"><active value=\"true\"/><gender value=\"bogus\"/></Patient>"u8.ToArray();
        var httpContext = GetHttpContext(contentBytes, DefaultContentType);

        var formatterContext = CreateInputFormatterContext(typeof(FhirModel.Resource), httpContext);

        SparkException exception = await Assert.ThrowsAsync<SparkException>(() => formatter.ReadAsync(formatterContext));
        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.NotNull(exception.Outcome);
        Assert.Contains(exception.Outcome.Issue, issue => issue.Expression.Contains("Patient.gender"));
    }

    [Fact]
    public async Task ReadAsync_ThrowsSparkException_BadRequest_OnMalformedBody()
    {
        var formatter = GetInputFormatter();

        var contentBytes = "this is not xml"u8.ToArray();
        var httpContext = GetHttpContext(contentBytes, DefaultContentType);

        var formatterContext = CreateInputFormatterContext(typeof(FhirModel.Resource), httpContext);

        SparkException exception = await Assert.ThrowsAsync<SparkException>(() => formatter.ReadAsync(formatterContext));
        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
    }

    private static AsyncResourceXmlInputFormatter GetInputFormatter(DeserializerSettings parserSettings = null)
    {
        if (parserSettings == null) parserSettings = new DeserializerSettings().UsingMode(DeserializationMode.Strict);
        return new AsyncResourceXmlInputFormatter(
            new FhirXmlDeserializer(parserSettings));
    }
}
