/* 
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Engine.Core;
using System.Linq;
using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;
using Xunit;

namespace Spark.Engine.Tests.Core;

public class FhirModelTests
{
    private FhirModel _model = new();

    [Fact]
    public void TestCompartments()
    {
        var actual = _model.FindCompartmentInfo(ResourceType.Patient.GetLiteral());

        Assert.NotNull(actual);
        Assert.True(actual.ReverseIncludes.Any());
    }

#if STU3_TESTS || R4_TESTS || R4B_TESTS
    [Fact]
    public void FindSearchParameters_UsesGeneratedGenericParameterDefinitions()
    {
        var genericParameters = _model.FindSearchParameters("Patient")
            .Where(parameter => new[] { "_id", "_lastUpdated", "_tag", "_profile", "_security", "_source" }.Contains(parameter.Name))
            .ToDictionary(parameter => parameter.Name, parameter => parameter.Type);

        Assert.Equal(6, genericParameters.Count);
        Assert.Equal(SearchParamType.Token, genericParameters["_id"]);
        Assert.Equal(SearchParamType.Date, genericParameters["_lastUpdated"]);
        Assert.Equal(SearchParamType.Token, genericParameters["_tag"]);
        Assert.Equal(SearchParamType.Uri, genericParameters["_profile"]);
        Assert.Equal(SearchParamType.Token, genericParameters["_security"]);
        Assert.Equal(SearchParamType.Uri, genericParameters["_source"]);
    }
#endif

#if R5_TESTS || R6_TESTS
    [Fact]
    public void FindSearchParameters_UsesGeneratedR5OrLaterGenericParameterDefinitions()
    {
        var genericParameters = _model.FindSearchParameters("Patient")
            .Where(parameter => new[] { "_id", "_lastUpdated", "_tag", "_profile", "_security", "_source" }.Contains(parameter.Name))
            .ToDictionary(parameter => parameter.Name, parameter => parameter.Type);

        Assert.Equal(6, genericParameters.Count);
        Assert.Equal(SearchParamType.Token, genericParameters["_id"]);
        Assert.Equal(SearchParamType.Date, genericParameters["_lastUpdated"]);
        Assert.Equal(SearchParamType.Token, genericParameters["_tag"]);
        Assert.Equal(SearchParamType.Reference, genericParameters["_profile"]);
        Assert.Equal(SearchParamType.Token, genericParameters["_security"]);
        Assert.Equal(SearchParamType.Uri, genericParameters["_source"]);
    }
#endif
}
