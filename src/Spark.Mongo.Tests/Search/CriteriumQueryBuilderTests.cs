/*
 * Copyright (c) 2021-2024, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Spark.Search;
using Spark.Search.Mongo;
using System.Linq;
using Xunit;

namespace Spark.Mongo.Tests.Search;

public class CriteriumQueryBuilderTests
{
    [Theory]
    [InlineData(ResourceType.Condition, "code", "code=ha125", "{ \"$or\" : [{ \"code\" : { \"$elemMatch\" : { \"code\" : \"ha125\" } } }, { \"code\" : { \"$not\" : { \"$type\" : 4 } }, \"code.code\" : \"ha125\" }, { \"$and\" : [{ \"code\" : { \"$type\" : 2 } }, { \"code\" : \"ha125\" }] }] }")]
    [InlineData(ResourceType.Condition, "code", "code=|ha125", "{ \"$or\" : [{ \"code\" : { \"$elemMatch\" : { \"code\" : \"ha125\", \"system\" : { \"$exists\" : false } } } }, { \"code\" : { \"$not\" : { \"$type\" : 4 } }, \"code.code\" : \"ha125\", \"code.system\" : { \"$exists\" : false } }, { \"$and\" : [{ \"code\" : { \"$type\" : 2 } }, { \"code\" : \"ha125\" }, { \"system\" : { \"$exists\" : false } }] }] }")]
    [InlineData(ResourceType.Condition, "code", "code:text=headache", "{ \"code.text\" : /headache/i }")]
    [InlineData(ResourceType.Patient, 
        "gender", 
        "gender:not=male",
        "{ \"$and\" : [{ \"gender\" : { \"$not\" : { \"$elemMatch\" : { \"code\" : \"male\" } } } }, { \"$nor\" : [{ \"gender\" : { \"$not\" : { \"$type\" : 4 } }, \"gender.code\" : \"male\" }] }, { \"$nor\" : [{ \"$and\" : [{ \"gender\" : { \"$type\" : 2 } }, { \"gender\" : \"male\" }] }] }] }")]
    [InlineData(ResourceType.Patient, "gender", "gender:missing=true", "{ \"gender\" : null, \"gender.text\" : null }")]
    [InlineData(ResourceType.Patient, "gender", "gender:missing=false", "{ \"$or\" : [{ \"gender\" : { \"$ne\" : null } }, { \"gender.text\" : null }] }")]
    public void Can_Build_TokenQuery_Filter(ResourceType resourceType, string searchParameter, string query, string expected)
    {
        var jsonFilter = BuildAndReturnQueryFilterAsJsonString(resourceType, searchParameter, query);

        Assert.Equal(expected, jsonFilter);
    }

    [Theory]
    [InlineData(ResourceType.RiskAssessment, "probability", "probability=0.8", "{ \"probability\" : \"0.8\" }")]
    [InlineData(ResourceType.RiskAssessment, "probability", "probability=eq0.8", "{ \"probability\" : \"0.8\" }")]
    [InlineData(ResourceType.RiskAssessment, "probability", "probability=gt0.8", "{ \"probability\" : { \"$gt\" : \"0.8\" } }")]
    [InlineData(ResourceType.RiskAssessment, "probability", "probability=ge0.8", "{ \"probability\" : { \"$gte\" : \"0.8\" } }")]
    [InlineData(ResourceType.RiskAssessment, "probability", "probability=lt0.8", "{ \"probability\" : { \"$lt\" : \"0.8\" } }")]
    [InlineData(ResourceType.RiskAssessment, "probability", "probability=le0.8", "{ \"probability\" : { \"$lte\" : \"0.8\" } }")]
    [InlineData(ResourceType.RiskAssessment, "probability", "probability=ne0.8", "{ \"probability\" : { \"$ne\" : \"0.8\" } }")]
    [InlineData(ResourceType.RiskAssessment, "probability", "probability:missing=true", "{ \"$or\" : [{ \"probability\" : { \"$exists\" : false } }, { \"probability\" : null }] }")]
    [InlineData(ResourceType.RiskAssessment, "probability", "probability:missing=false", "{ \"probability\" : { \"$ne\" : null } }")]
    public void Can_Build_NumberQuery_Filter(ResourceType resourceType, string searchParameter, string query, string expected)
    {
        var jsonFilter = BuildAndReturnQueryFilterAsJsonString(resourceType, searchParameter, query);

        Assert.Equal(expected, jsonFilter);
    }

    [Theory]
    [InlineData(ResourceType.Patient, "name", "name=eve", "{ \"name\" : /^eve/i }")]
    [InlineData(ResourceType.Patient, "name", "name:contains=eve", "{ \"name\" : /.*eve.*/i }")]
    [InlineData(ResourceType.Patient, "name", "name:exact=Eve", "{ \"name\" : \"Eve\" }")]
    [InlineData(ResourceType.Patient, "name", "name:missing=true", "{ \"$or\" : [{ \"name\" : { \"$exists\" : false } }, { \"name\" : null }] }")]
    [InlineData(ResourceType.Patient, "name", "name:missing=false", "{ \"name\" : { \"$ne\" : null } }")]
    // Complex cases or edge cases
    [InlineData(
        ResourceType.Subscription,
        "criteria",
        "criteria=Observation?patient.identifier=http://somehost.no/fhir/Name%20Hospital|someId",
        "{ \"criteria\" : /^Observation?patient.identifier=http:\\/\\/somehost.no\\/fhir\\/Name%20Hospital|someId/i }")]
    public void Can_Build_StringQuery_Filter(ResourceType resourceType, string searchParameter, string query, string expected)
    {
        var jsonFilter = BuildAndReturnQueryFilterAsJsonString(resourceType, searchParameter, query);

        Assert.Equal(expected, jsonFilter);
    }

    [Theory]
    [InlineData(ResourceType.Procedure, "date", "date=2010-01-01", "{ \"date.end\" : { \"$gte\" : ISODate(\"2010-01-01T00:00:00Z\") }, \"date.start\" : { \"$lt\" : ISODate(\"2010-01-02T00:00:00Z\") } }")]
    [InlineData(ResourceType.Procedure, "date", "date=ap2010-01-01", "{ \"date.end\" : { \"$gte\" : ISODate(\"2010-01-01T00:00:00Z\") }, \"date.start\" : { \"$lt\" : ISODate(\"2010-01-02T00:00:00Z\") } }")]
    [InlineData(ResourceType.Procedure, "date", "date=eq2010-01-01", "{ \"date.end\" : { \"$gte\" : ISODate(\"2010-01-01T00:00:00Z\") }, \"date.start\" : { \"$lt\" : ISODate(\"2010-01-02T00:00:00Z\") } }")]
    [InlineData(ResourceType.Procedure, "date", "date=ne2010-01-01", "{ \"$or\" : [{ \"date.end\" : { \"$lte\" : ISODate(\"2010-01-01T00:00:00Z\") } }, { \"date.start\" : { \"$gte\" : ISODate(\"2010-01-02T00:00:00Z\") } }] }")]
    [InlineData(ResourceType.Procedure, "date", "date=gt2010-01-01", "{ \"date.start\" : { \"$gte\" : ISODate(\"2010-01-02T00:00:00Z\") } }")]
    [InlineData(ResourceType.Procedure, "date", "date=ge2010-01-01", "{ \"date.start\" : { \"$gte\" : ISODate(\"2010-01-01T00:00:00Z\") } }")]
    [InlineData(ResourceType.Procedure, "date", "date=lt2010-01-01", "{ \"date.end\" : { \"$lt\" : ISODate(\"2010-01-01T00:00:00Z\") } }")]
    [InlineData(ResourceType.Procedure, "date", "date=le2010-01-01", "{ \"date.end\" : { \"$lte\" : ISODate(\"2010-01-02T00:00:00Z\") } }")]
    [InlineData(ResourceType.Procedure, "date", "date=sa2010-01-01", "{ \"date.start\" : { \"$gte\" : ISODate(\"2010-01-02T00:00:00Z\") } }")]
    [InlineData(ResourceType.Procedure, "date", "date=eb2010-01-01", "{ \"date.end\" : { \"$lte\" : ISODate(\"2010-01-01T00:00:00Z\") } }")]
    [InlineData(ResourceType.Procedure, "date", "date:missing=true", "{ \"$or\" : [{ \"date\" : { \"$exists\" : false } }, { \"date\" : null }] }")]
    [InlineData(ResourceType.Procedure, "date", "date:missing=false", "{ \"date\" : { \"$ne\" : null } }")]
    public void Can_Build_DateQuery_Filter(ResourceType resourceType, string searchParameter, string query, string expected)
    {
        var jsonFilter = BuildAndReturnQueryFilterAsJsonString(resourceType, searchParameter, query);

        Assert.Equal(expected, jsonFilter);
    }

    [Theory]
    [InlineData(ResourceType.CodeSystem, "url", "url=http://CodeSystem.fhir.org/test", "{ \"url\" : \"http://codesystem.fhir.org/test\" }")]
    [InlineData(ResourceType.CodeSystem, "url", "url=http://CodeSystem.fhir.org/test/Test", "{ \"url\" : \"http://codesystem.fhir.org/test/Test\" }")]
    [InlineData(ResourceType.CodeSystem, "url", "url=http://CodeSystem.fhir.org/Test", "{ \"url\" : \"http://codesystem.fhir.org/Test\" }")]
    public void Can_Build_UriQuery_Filter(ResourceType resourceType, string searchParameter, string query, string expected)
    {
        var jsonFilter = BuildAndReturnQueryFilterAsJsonString(resourceType, searchParameter, query);

        Assert.Equal(expected, jsonFilter);
    }

    private string BuildAndReturnQueryFilterAsJsonString(ResourceType resourceType, string searchParameter, string query)
    {
        var bsonSerializerRegistry = new BsonSerializerRegistry();
        bsonSerializerRegistry.RegisterSerializationProvider(new BsonSerializationProvider());

        var resourceTypeAsString = resourceType.GetLiteral();
        var keyVal = query.SplitLeft('=');
        if (keyVal.Item2 == null) throw Error.Argument("text", "Value must contain an '=' to separate key and value");
        var criterium = Criterium.Parse(resourceTypeAsString, keyVal.Item1, keyVal.Item2);
        criterium.SearchParameters.AddRange(ModelInfo.SearchParameters.Where(p => p.Resource == resourceTypeAsString && p.Name == searchParameter));

        var filter = criterium.ToFilter(resourceType.GetLiteral());
        var jsonFilter = filter.Render(new RenderArgs<BsonDocument>(bsonSerializerRegistry.GetSerializer<BsonDocument>(), bsonSerializerRegistry)).ToJson();

        return jsonFilter;
    }
}
