using Microsoft.AspNetCore.Http;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.FhirResponseFactory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Spark.Engine.Test.FhirResponseFactory;

public class ConditionalHeaderFhirResponseInterceptorTests
{
    [Fact]
    public void TestNotModified()
    {
        var versionId = "1";
        var etag = ETag.Create(versionId);
        var lastUpdated = DateTimeOffset.UtcNow.AddMinutes(-5);
        var context = new DefaultHttpContext();
        context.Request.Headers["If-None-Match"] = etag;
        context.Request.Headers["If-Modified-Since"] = lastUpdated.ToString("R");
        var parameters = new ConditionalHeaderParameters(context.Request);

        Assert.Equal(parameters.IfModifiedSince.Value.ToString("R"), lastUpdated.ToString("R"));
        Assert.Contains(parameters.IfNoneMatchTags, i => i == etag);

        var patient = new Hl7.Fhir.Model.Patient
        {
            Id = "1",
            Meta = new Hl7.Fhir.Model.Meta
            {
                VersionId = versionId,
                LastUpdated = lastUpdated
            }
        };

        var interceptor = new ConditionalHeaderFhirResponseInterceptor();
        var response = interceptor.GetFhirResponse(Entry.PUT(Key.Create(patient.TypeName, patient.Id, patient.Meta.VersionId), patient), parameters);
        Assert.NotNull(response);
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.NotModified);
    }


    [Fact]
    public void TestModified()
    {
        var versionId = "1";
        var etag = ETag.Create(versionId);
        var lastUpdated = DateTimeOffset.UtcNow.AddMinutes(-5);
        var context = new DefaultHttpContext();
        context.Request.Headers["If-None-Match"] = etag;
        context.Request.Headers["If-Modified-Since"] = lastUpdated.ToString("R");
        var parameters = new ConditionalHeaderParameters(context.Request);

        Assert.Equal(parameters.IfModifiedSince.Value.ToString("R"), lastUpdated.ToString("R"));
        Assert.Contains(parameters.IfNoneMatchTags, i => i == etag);

        var patient = new Hl7.Fhir.Model.Patient
        {
            Id = "1",
            Meta = new Hl7.Fhir.Model.Meta
            {
                VersionId = "2",
                LastUpdated = DateTimeOffset.UtcNow
            }
        };

        var interceptor = new ConditionalHeaderFhirResponseInterceptor();
        var response = interceptor.GetFhirResponse(Entry.PUT(Key.Create(patient.TypeName, patient.Id, patient.Meta.VersionId), patient), parameters);
        Assert.Null(response);
    }
}
