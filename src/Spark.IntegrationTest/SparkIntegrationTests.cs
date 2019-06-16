//using Microsoft.AspNetCore.Mvc.Testing;
//using System;
//using System.Net.Http;
//using System.Net.Http.Headers;
//using System.Threading.Tasks;
//using Xunit;

//namespace Spark.IntegrationTest
//{
//    public class SparkIntegrationTests
//        : IClassFixture<WebApplicationFactory<NetCore.Startup>>
//    {
//        private readonly WebApplicationFactory<NetCore.Startup> _factory;

//        public SparkIntegrationTests(WebApplicationFactory<NetCore.Startup> factory)
//        {
//            _factory = factory;
//        }

//        [Theory]
//        [InlineData("/fhir/Patient/example", "application/xml")]
//        [InlineData("/fhir/Patient/example", "application/fhir+xml")]
//        [InlineData("/fhir/Patient/example", "application/xml+fhir")]
//        [InlineData("/fhir/Patient/example", "text/xml")]
//        [InlineData("/fhir/Patient/example", "application/json")]
//        [InlineData("/fhir/Patient/example", "application/json+fhir")]
//        [InlineData("/fhir/Patient/example", "application/fhir+json")]
//        [InlineData("/fhir/Patient/example", "text/json")]
//        public async Task Read_ReturnsSuccessAndCorrectContentType(string url, string contentType)
//        {
//            // Arrange
//            HttpClient client = _factory.CreateClient();

//            // Act
//            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));
//            var response = await client.GetAsync(url);

//            // Assert
//            Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK); // Status Code 200
//            Assert.Equal($"{contentType}; charset=utf-8", response.Content.Headers.ContentType.ToString());
//        }

//        [Theory]
//        [InlineData("/fhir/Patient/example/_history/1", "application/xml")]
//        [InlineData("/fhir/Patient/example/_history/1", "application/fhir+xml")]
//        [InlineData("/fhir/Patient/example/_history/1", "application/json")]
//        [InlineData("/fhir/Patient/example/_history/1", "application/fhir+json")]
//        public async Task VRead_ReturnsSuccessAndCorrectContentType(string url, string contentType)
//        {
//            // Arrange
//            HttpClient client = _factory.CreateClient();

//            // Act
//            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));
//            var response = await client.GetAsync(url);

//            // Assert
//            Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK); // Status Code 200
//            Assert.Equal($"{contentType}; charset=utf-8", response.Content.Headers.ContentType.ToString());
//        }

//        public async Task Create_Returns201CorrectContentTypeAndResource(string url, string contentType)
//        {

//        }

//        public async Task ConditionalCreate_Returns201CorrectContentTypeAndResource(string url, string contentType)
//        {

//        }

//        public async Task Update_Returns200Or201CorrectContentTypeAndResource(string url, string contentType)
//        {

//        }

//        public async Task ConditionalUpdate_Returns200Or201CorrectContentTypeAndResource(string url, string contentType)
//        {

//        }

//        public async Task Delete_ReturnsSuccessAndCorrectContentType(string url, string contentType)
//        {

//        }

//        public async Task ConditionalDelete_ReturnsSuccessAndCorrectContentType(string url, string contentType)
//        {

//        }

//        public async Task InstanceSearch_ReturnsSuccessAndCorrectContentType(string url, string contentType)
//        {

//        }

//        public async Task WholeSystemSearch_ReturnsSuccessAndCorrectContentType(string url, string contentType)
//        {

//        }

//        public async Task GetConformance_ReturnsSuccessAndCorrectContentType(string url, string contentType)
//        {

//        }
//    }
//}
