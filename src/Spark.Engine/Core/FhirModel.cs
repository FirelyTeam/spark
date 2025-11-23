/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2017-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;
using Spark.Engine.Extensions;
using Spark.Engine.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using static Hl7.Fhir.Model.ModelInfo;

namespace Spark.Engine.Core;

public class FhirModel : IFhirModel
{
    private readonly List<CompartmentInfo> _compartments = [];
    private List<SearchParameter> _searchParameters;

    // FIXME: This and the internal constructor below should be removed when IndexServiceTests use mocking instead of
    // overriding resource type mapping and the available SearchParamDefinitions through the constructor.
    private readonly Dictionary<Type, string> _resourceTypeToResourceTypeName;

    // This method is only supposed to be accessed by tests and is therefore be marked as internal.
    internal FhirModel(Dictionary<Type, string> resourceTypeToResourceTypeNameMapping, IEnumerable<SearchParamDefinition> searchParameters)
    {
        _resourceTypeToResourceTypeName = resourceTypeToResourceTypeNameMapping;
        LoadSearchParameters(searchParameters);
        LoadCompartments();
    }

    public FhirModel() : this(ModelInfo.SearchParameters)
    {
    }

    public FhirModel(IEnumerable<SearchParamDefinition> searchParameters)
    {
        LoadSearchParameters(searchParameters);
        LoadCompartments();
    }

    private void LoadSearchParameters(IEnumerable<SearchParamDefinition> searchParameters)
    {
        _searchParameters = searchParameters.Select(createSearchParameterFromSearchParamDefinition).ToList();
        LoadGenericSearchParameters();
    }

    private void LoadGenericSearchParameters()
    {
        List<SearchParamDefinition> genericSearchParamDefinitions =
        [
            new()
            {
                Resource = "Resource",
                Name = "_id",
                Type = SearchParamType.String,
                Expression = "Resource.id",
                Path = ["Resource.id"]
            },
            new()
            {
                Resource = "Resource",
                Name = "_lastUpdated",
                Type = SearchParamType.Date,
                Expression = "Resource.meta.lastUpdated",
                Path = ["Resource.meta.lastUpdated"]
            },
            new()
            {
                Resource = "Resource",
                Name = "_profile",
                Type = SearchParamType.Uri,
                Expression = "Resource.meta.profile",
                Path = ["Resource.meta.profile"]
            },
            new()
            {
                Resource = "Resource",
                Name = "_security",
                Type = SearchParamType.Token,
                Expression = "Resource.meta.security",
                Path = ["Resource.meta.security"]
            },
            new()
            {
                Resource = "Resource",
                Name = "_tag",
                Type = SearchParamType.Token,
                Expression = "Resource.meta.tag",
                Path = ["Resource.meta.tag"]
            }
        ];
        var genericSearchParameters =
            genericSearchParamDefinitions.Select(createSearchParameterFromSearchParamDefinition);

        // NOTE: We have no control over the incoming list of searchParameters (in the constructor), so these generic
        // parameters may or may not be in there. Therefore, apply the Except operation to make sure these parameters
        // are not added twice.
        _searchParameters.AddRange(genericSearchParameters.Except(_searchParameters));
    }

    private SearchParameter createSearchParameterFromSearchParamDefinition(SearchParamDefinition def)
    {
        SearchParameter result = new SearchParameter
        {
            Resource = def.Resource,
            // SearchParamDefinition has no Code, but in all current SearchParameter resources, name and code are equal.
            Name = def.Name, Code = def.Name,
            Base = [GetResourceTypeForResourceName(def.Resource)], Type = def.Type,
            Target = def.Target == null || def.Target.Length == 0
                ? []
                : GetResourceTypesForResourceNames(def.Target).ToArray(),
            Description = def.Description
        };
        // NOTE: This is a fix to handle an issue in the firely-net-sdk where the expression 'ConceptMap.source as uri'
        // returns a string instead of uri.
        // FIXME: On a longer term we should refactor the  SearchParameter in-memory cache so we can more elegantly swap
        // out a SearchParameter
        if (def.Resource == ResourceType.ConceptMap.GetLiteral())
        {
            result.Expression = def.Name switch
            {
                "source-uri" => "ConceptMap.source.as(uri)",
                "target-uri" => "ConceptMap.target.as(uri)",
                _ => def.Expression
            };
        }
        else
        {
            result.Expression = def.Expression;
        }

        // Strip off the [x], for example in Condition.onset[x].
        result.SetPropertyPath(def.Path?.Select(p => p.Replace("[x]", "")).ToArray());

        // NOTE: SearchParameter is not very good yet with Composite parameters. Therefore, we include a reference to
        // the original SearchParamDefinition.
        // FIXME: Need to confirm if the above NOTE is still true.
        result.OriginalDefinition = def;

        return result;
    }

    public IReadOnlyList<SearchParameter> SearchParameters => _searchParameters;

    public string GetResourceNameForType(Type type)
    {
        return _resourceTypeToResourceTypeName == null
            ?  GetFhirTypeNameForType(type)
            : _resourceTypeToResourceTypeName[type];
    }

    public Type GetTypeForResourceName(string name)
    {
        return GetTypeForFhirType(name);
    }

    private static VersionIndependentResourceTypesAll GetResourceTypeForResourceName(string name)
    {
        return Enum.Parse<VersionIndependentResourceTypesAll>(name, true);
    }

    private static IEnumerable<VersionIndependentResourceTypesAll> GetResourceTypesForResourceNames(ResourceType[] names)
    {
        return names?.Select(name => GetResourceTypeForResourceName(name.GetLiteral()));
    }

    public IEnumerable<SearchParameter> FindSearchParameters(Type resourceType)
    {
        return FindSearchParameters(GetResourceNameForType(resourceType));
    }

    public IEnumerable<SearchParameter> FindSearchParameters(string resourceName)
    {
        return SearchParameters.Where(sp => sp.Base.Contains(GetResourceTypeForResourceName(resourceName)) || sp.Base.Any(b => b == VersionIndependentResourceTypesAll.Resource));
    }

    public SearchParameter FindSearchParameter(Type resourceType, string parameterName)
    {
        return FindSearchParameter(GetResourceNameForType(resourceType), parameterName);
    }

    public SearchParameter FindSearchParameter(string resourceName, string parameterName) => FindSearchParameters(resourceName).FirstOrDefault(sp => sp.Name == parameterName);

    public string GetLiteralForEnum(Enum value)
    {
        return value.GetLiteral();
    }

    private void LoadCompartments()
    {
        // FIXME: This might be better resolved through a CompartmentDefinition.
        var searchParameters = SearchParameters.Where(searchParameter =>
            searchParameter.Type == SearchParamType.Reference
            && searchParameter.Target.Contains(VersionIndependentResourceTypesAll.Patient)
            && !searchParameter.Base.Any(IsDefinitionResourceType)
            && searchParameter.Name != "subject");

        var reverseIncludes = new List<string>();
        foreach (SearchParameter searchParameter in searchParameters)
        {
            reverseIncludes.AddRange(
                from VersionIndependentResourceTypesAll? resourceType in searchParameter.Base
                where resourceType.HasValue
                select $"{resourceType.GetLiteral()}:{searchParameter.Name}"
            );
        }

        CompartmentInfo patientCompartmentInfo = new(ResourceType.Patient.GetLiteral());
        patientCompartmentInfo.AddReverseIncludes(reverseIncludes);
        _compartments.Add(patientCompartmentInfo);
    }

    private static bool IsDefinitionResourceType(VersionIndependentResourceTypesAll resourceType)
    {
        return DefinitionResourceTypes().Contains(resourceType);
    }

    private static IEnumerable<VersionIndependentResourceTypesAll> DefinitionResourceTypes()
    {
        return
        [
            VersionIndependentResourceTypesAll.ActivityDefinition,
            VersionIndependentResourceTypesAll.CompartmentDefinition,
            VersionIndependentResourceTypesAll.GraphDefinition,
            VersionIndependentResourceTypesAll.MessageDefinition,
            VersionIndependentResourceTypesAll.OperationDefinition,
            VersionIndependentResourceTypesAll.PlanDefinition,
            VersionIndependentResourceTypesAll.ServiceDefinition,
            VersionIndependentResourceTypesAll.StructureDefinition
        ];
    }

    public CompartmentInfo FindCompartmentInfo(string resourceType) =>
        _compartments.FirstOrDefault(ci => ci.ResourceType == resourceType);
}
