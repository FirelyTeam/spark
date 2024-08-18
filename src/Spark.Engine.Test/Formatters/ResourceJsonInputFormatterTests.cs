/* 
 * Copyright (c) 2020-2024, Incendi (info@incendi.no)
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using FhirModel = Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Http;
using Spark.Engine.Core;
using Spark.Engine.Formatters;
using Spark.Engine.Test.Utility;
using System.Buffers;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Spark.Engine.Test.Formatters
{
    public class ResourceJsonInputFormatterTests : FormatterTestBase
    {
        private const string DEFAULT_CONTENT_TYPE = "application/json";

        [Theory]
        [InlineData("application/fhir+json", true)]
        [InlineData("application/json+fhir", true)]
        [InlineData("application/json", true)]
        [InlineData("application/*", false)]
        [InlineData("*/*", false)]
        [InlineData("text/json", true)]
        [InlineData("text/*", false)]
        [InlineData("text/xml", false)]
        [InlineData("application/xml", false)]
        [InlineData("application/some.entity+json", true)]
        [InlineData("application/some.entity+json;v=2", true)]
        [InlineData("application/some.entity+xml", false)]
        [InlineData("application/some.entity+*", false)]
        [InlineData("text/some.entity+json", true)]
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

            Assert.Equal("application/fhir+json", mediaType.ToString());
        }

        [Fact]
        public async Task ReadAsync_RequestBody_IsBuffered_And_IsSeekable()
        {
            var formatter = GetInputFormatter();

            var fhirVersionMoniker = FhirVersionUtility.GetFhirVersionMoniker();
            var content = GetResourceFromFileAsString(Path.Combine("TestData", fhirVersionMoniker.ToString(), "patient-example.json"));
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

        [Fact]
        public async Task ReadAsync_ThrowsSparkException_BadRequest_OnNonUtf8Content()
        {
            var formatter = GetInputFormatter();

            var content = "ɊɋɌɍɎɏ";
            var contentBytes = Encoding.Unicode.GetBytes(content);

            var httpContext = GetHttpContext(contentBytes, DEFAULT_CONTENT_TYPE);

            var formatterContext = CreateInputFormatterContext(typeof(FhirModel.Resource), httpContext);

            SparkException exception = await Assert.ThrowsAsync<SparkException>(() => formatter.ReadAsync(formatterContext));
            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        }

        protected static ResourceJsonInputFormatter GetInputFormatter(ParserSettings parserSettings = null)
        {
            if (parserSettings == null) parserSettings = new ParserSettings { PermissiveParsing = false };
            return new ResourceJsonInputFormatter(
                new FhirJsonParser(parserSettings),
                ArrayPool<char>.Shared);
        }
    }
}
