/*
 * Copyright (c) 2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Spark.Engine;
using Spark.Engine.Extensions;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Web.Tests;

public class MultiFormatterSupportTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MultiFormatterSupportTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseDefaultServiceProvider(options =>
            {
                options.ValidateScopes = false;
                options.ValidateOnBuild = false;
            });
            builder.ConfigureServices(services =>
            {
                SparkSettings settings = new()
                {
                    Endpoint = new Uri("http://localhost"),
                    DeserializerSettings = new DeserializerSettings().UsingMode(DeserializationMode.Strict),
                    UseAsynchronousIO = true
                };
                services.AddFhir(settings);
                services.AddMvc(options =>
                {
                    options.EnableEndpointRouting = false;
                })
                .AddApplicationPart(typeof(MultiFormatterSupportTest).Assembly);
            });

            builder.Configure(app =>
            {
                app.UseFhir();
            });
        });
    }

    [Fact]
    public async Task Post_SampleJson()
    {
        HttpClient client = _factory.CreateClient();
        StringContent content = new("{\"name\":\"test\"}", Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync("api/test/test-json", content, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        string responseString = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal("test", responseString);
    }

    [Fact]
    public async Task Post_SampleFile()
    {
        HttpClient client = _factory.CreateClient();
        MultipartFormDataContent content = new();
        ByteArrayContent fileContent = new("empty file."u8.ToArray());
        content.Add(fileContent, "file", "test.txt");
        HttpResponseMessage response = await client.PostAsync("api/test/test-file", content, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        string responseString = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("Name: test.txt, Content: empty file.", responseString);
    }

    [Fact]
    public async Task Post_JsonFile()
    {
        HttpClient client = _factory.CreateClient();
        MultipartFormDataContent content = new();
        ByteArrayContent fileContent = new("empty file."u8.ToArray());
        content.Add(fileContent, "file", "test.txt");
        content.Add(new StringContent("Test Property"), "other");

        HttpResponseMessage response = await client.PostAsync("api/test/test-json-file", content, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        string responseString = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("Test Property", responseString);
    }

    [Fact]
    public async Task Post_FhirJson()
    {
        HttpClient client = _factory.CreateClient();
        StringContent content = new("{\"resourceType\":\"Basic\",\"id\":\"1\",\"code\":{\"coding\":[{\"system\":\"http://terminology.hl7.org/CodeSystem/basic-resource-type\",\"code\": \"referral\"}]}}", Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync("api/test/test-resource", content, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        string responseString = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal("1", responseString);
    }

    [Fact]
    public async Task Post_FhirXml()
    {
        HttpClient client = _factory.CreateClient();
        StringContent content = new("<Basic xmlns=\"http://hl7.org/fhir\"><id value=\"1\"/><code><coding><system value=\"http://terminology.hl7.org/CodeSystem/basic-resource-type\"/><code value=\"referral\"/></coding></code></Basic>", Encoding.UTF8,
            "application/xml");
        HttpResponseMessage response = await client.PostAsync("api/test/test-resource", content, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        string responseString = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal("1", responseString);
    }
}
