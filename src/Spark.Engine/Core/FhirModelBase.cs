/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;
using Spark.Engine.Extensions;
using Spark.Engine.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using SearchParameter = Spark.Engine.Model.SearchParameter;

namespace Spark.Engine.Core;

public abstract class FhirModelBase : IFhirModel
{
    private readonly List<CompartmentInfo> _compartments = [];
    private List<SearchParameter> _searchParameters;

    // FIXME: This and the internal constructor below should be removed when IndexServiceTests use mocking instead of
    // overriding resource type mapping and the available SearchParamDefinitions through the constructor.
    private readonly Dictionary<Type, string> _resourceTypeToResourceTypeName;

    // This constructor is only supposed to be accessed by tests and is therefore marked as internal.
    internal FhirModelBase(Dictionary<Type, string> resourceTypeToResourceTypeNameMapping, IEnumerable<ModelInfo.SearchParamDefinition> searchParameters)
    {
        _resourceTypeToResourceTypeName = resourceTypeToResourceTypeNameMapping;
        LoadSearchParameters(searchParameters);
        LoadCompartments();
    }

    protected FhirModelBase(IEnumerable<ModelInfo.SearchParamDefinition> searchParameters)
    {
        LoadSearchParameters(searchParameters);
        LoadCompartments();
    }

    private void LoadSearchParameters(IEnumerable<ModelInfo.SearchParamDefinition> searchParameters)
    {
        _searchParameters = searchParameters.Select(CreateSearchParameterFromSearchParamDefinition).ToList();
        LoadGenericSearchParameters();
    }

    private void LoadGenericSearchParameters()
    {
        List<(string Name, SearchParamType Type, string Expression)> genericDefs =
        [
            ("_id", Type: SearchParamType.String, "Resource.id"),
            ("_lastUpdated", Type: SearchParamType.Date, "Resource.meta.lastUpdated"),
            ("_profile", Type: SearchParamType.Uri, "Resource.meta.profile"),
            ("_security", Type: SearchParamType.Token, "Resource.meta.security"),
            ("_tag", Type: SearchParamType.Token, "Resource.meta.tag"),
        ];

        var genericSearchParameters = genericDefs.Select(d =>
        {
            var sp = new SearchParameter
            {
                Resource = "Resource",
                Name = d.Name,
                Code = d.Name,
                Base = [VersionIndependentResourceTypesAll.Resource],
                Type = d.Type,
                Target = [],
                Component = [],
                Expression = d.Expression,
                Path = [d.Expression]
            };
            sp.SetPropertyPath([d.Expression]);
            return sp;
        });

        // NOTE: The incoming list of searchParameters may already contain these generic parameters,
        //       so use Except to ensure they are not added twice.
        _searchParameters.AddRange(genericSearchParameters.Except(_searchParameters));
    }

    private static SearchParameter CreateSearchParameterFromSearchParamDefinition(ModelInfo.SearchParamDefinition def)
    {
        SearchParameter sp = new()
        {
            Resource = def.Resource,
            // SearchParamDefinition has no Code, but in all current SearchParameter resources, name and code are equal.
            Name = def.Name,
            Code = def.Name,
            Base = [GetResourceTypeForResourceName(def.Resource)],
            Type = def.Type,
            Target = def.Target?.Select(rt => Enum.Parse<VersionIndependentResourceTypesAll>(rt.GetLiteral())).ToArray() ?? [],
            Description = def.Description,
            Expression = def.Expression,
            Component = def.Component == null
                ? []
                : def.Component.Select(c => new SearchParameterComponent(c.Definition, c.Expression)).ToArray(),
        };

        // Strip off the [x], for example in Condition.onset[x].
        var paths = def.Path?.Select(p => p.Replace("[x]", "")).ToArray() ?? [];
        sp.Path = paths;
        sp.SetPropertyPath(paths);

        return sp;
    }

    public IReadOnlyList<SearchParameter> SearchParameters => _searchParameters;

    public abstract IReadOnlyList<string> SupportedResources { get; }

    public abstract string FhirRelease { get; }

    public abstract ModelInspector GetModelInspector();

    public abstract Type GetTypeForFhirType(string typeName);

    public abstract string GetFhirTypeNameForType(Type type);

    public string GetResourceNameForType(Type type)
    {
        return _resourceTypeToResourceTypeName == null
            ? GetFhirTypeNameForType(type)
            : _resourceTypeToResourceTypeName[type];
    }

    public Type GetTypeForResourceName(string name) => GetTypeForFhirType(name);

    private static VersionIndependentResourceTypesAll GetResourceTypeForResourceName(string name)
    {
        return Enum.Parse<VersionIndependentResourceTypesAll>(name, true);
    }

    public IEnumerable<SearchParameter> FindSearchParameters(Type resourceType)
    {
        return FindSearchParameters(GetResourceNameForType(resourceType));
    }

    public IEnumerable<SearchParameter> FindSearchParameters(string resourceName)
    {
        return SearchParameters.Where(sp =>
            sp.Base.Contains(GetResourceTypeForResourceName(resourceName))
            || sp.Base.Any(b => b == VersionIndependentResourceTypesAll.Resource));
    }

    public SearchParameter FindSearchParameter(Type resourceType, string parameterName)
    {
        return FindSearchParameter(GetResourceNameForType(resourceType), parameterName);
    }

    public SearchParameter FindSearchParameter(string resourceName, string parameterName) =>
        FindSearchParameters(resourceName).FirstOrDefault(sp => sp.Name == parameterName);

    public string GetLiteralForEnum(Enum value) => value.GetLiteral();

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

        CompartmentInfo patientCompartmentInfo = new("Patient");
        patientCompartmentInfo.AddReverseIncludes(reverseIncludes);
        _compartments.Add(patientCompartmentInfo);
    }

    private bool IsDefinitionResourceType(VersionIndependentResourceTypesAll resourceType)
    {
        return DefinitionResourceTypes().Contains(resourceType);
    }

    protected abstract IEnumerable<VersionIndependentResourceTypesAll> DefinitionResourceTypes();

    public CompartmentInfo FindCompartmentInfo(string resourceType) =>
        _compartments.FirstOrDefault(ci => ci.ResourceType == resourceType);
}
