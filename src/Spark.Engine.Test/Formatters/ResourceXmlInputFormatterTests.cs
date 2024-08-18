/* 
 * Copyright (c) 2020-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using FhirModel = Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Http;
using Spark.Engine.Formatters;
using Spark.Engine.Test.Utility;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Spark.Engine.Test.Formatters
{
    public class ResourceXmlInputFormatterTests : FormatterTestBase
    {
        private const string DEFAULT_CONTENT_TYPE = "application/xml";

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

            Assert.Equal("application/fhir+xml", mediaType.ToString());
        }

        [Fact]
        public async Task ReadAsync_RequestBody_IsBuffered_And_IsSeekable()
        {
            var formatter = GetInputFormatter();

            var fhirVersionMoniker = FhirVersionUtility.GetFhirVersionMoniker();
            var content = GetResourceFromFileAsString(Path.Combine("TestData", fhirVersionMoniker.ToString(), "patient-example.xml"));
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = DEFAULT_CONTENT_TYPE;
            httpContext.Request.Body = new NonSeekableReadStream(contentBytes);

            var formatterContext = CreateInputFormatterContext(typeof(FhirModel.Resource), httpContext);

            var result = await formatter.ReadAsync(formatterContext);

            Assert.False(result.HasError);

            var patient = Assert.IsType<FhirModel.Patient>(result.Model);
            Assert.Equal("example", patient.Id);
            Assert.Equal(true, patient.Active);

            Assert.True(httpContext.Request.Body.CanSeek);
            httpContext.Request.Body.Seek(0L, SeekOrigin.Begin);

            // Try again

            result = await formatter.ReadAsync(formatterContext);

            Assert.False(result.HasError);

            patient = Assert.IsType<FhirModel.Patient>(result.Model);
            Assert.Equal("example", patient.Id);
            Assert.Equal(true, patient.Active);
        }

        protected static ResourceXmlInputFormatter GetInputFormatter(ParserSettings parserSettings = null)
        {
            if (parserSettings == null) parserSettings = new ParserSettings { PermissiveParsing = false };
            return new ResourceXmlInputFormatter(
                new FhirXmlParser(parserSettings));
        }
    }
}
