/*
 * Copyright (c) 2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Builder;
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
            builder.ConfigureServices(services =>
            {
                SparkSettings settings = new()
                {
                    Endpoint = new Uri("http://localhost"),
                    ParserSettings = new ParserSettings { PermissiveParsing = false },
                    UseAsynchronousIO = true
                };
                services.AddFhir(settings);
                services.AddMvc(options =>
                {
                    options.EnableEndpointRouting = false;
                });
            });

            builder.Configure(app =>
            {
                app.UseRouting();
                app.UseFhir();
            });
        });
    }

    [Fact]
    public async Task Post_SampleJson()
    {
        HttpClient client = _factory.CreateClient();
        StringContent content = new("{\"name\":\"test\"}", Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync("api/test/test-json", content);
        response.EnsureSuccessStatusCode();
        string responseString = await response.Content.ReadAsStringAsync();
        Assert.Equal("test", responseString);
    }

    [Fact]
    public async Task Post_SampleFile()
    {
        HttpClient client = _factory.CreateClient();
        MultipartFormDataContent content = new();
        ByteArrayContent fileContent = new("empty file."u8.ToArray());
        content.Add(fileContent, "file", "test.txt");
        HttpResponseMessage response = await client.PostAsync("api/test/test-file", content);
        response.EnsureSuccessStatusCode();
        string responseString = await response.Content.ReadAsStringAsync();
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

        HttpResponseMessage response = await client.PostAsync("api/test/test-json-file", content);
        response.EnsureSuccessStatusCode();
        string responseString = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Property", responseString);
    }

    [Fact]
    public async Task Post_FhirJson()
    {
        HttpClient client = _factory.CreateClient();
        StringContent content = new("{\"resourceType\":\"Basic\",\"id\":\"1\"}", Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync("api/test/test-resource", content);
        response.EnsureSuccessStatusCode();
        string responseString = await response.Content.ReadAsStringAsync();
        Assert.Equal("1", responseString);
    }


    [Fact]
    public async Task Post_FhirXml()
    {
        HttpClient client = _factory.CreateClient();
        StringContent content = new("<Basic xmlns=\"http://hl7.org/fhir\"><id value=\"1\"/></Basic>", Encoding.UTF8,
            "application/xml");
        HttpResponseMessage response = await client.PostAsync("api/test/test-resource", content);
        response.EnsureSuccessStatusCode();
        string responseString = await response.Content.ReadAsStringAsync();
        Assert.Equal("1", responseString);
    }
}
