/* 
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2017-2024, Incendi <info@incendi.no>
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
    //TODO: this should be removed after IndexServiceTests are changed to used mocking instead of this for overriding the context (CCR).
    private readonly Dictionary<Type, string> _resourceTypeToResourceTypeName;

    private List<SearchParameter> _searchParameters;
    public FhirModel(Dictionary<Type, string> resourceTypeToResourceTypeNameMapping, IEnumerable<SearchParamDefinition> searchParameters)
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
        _searchParameters = searchParameters.Select(sp => createSearchParameterFromSearchParamDefinition(sp)).ToList();
        LoadGenericSearchParameters();
    }

    private void LoadGenericSearchParameters()
    {
        var genericSearchParamDefinitions = new List<SearchParamDefinition>
        {
            new SearchParamDefinition { Resource = "Resource", Name = "_id", Type = SearchParamType.String, Expression = "Resource.id", Path = new string[] { "Resource.id" } }
            , new SearchParamDefinition { Resource = "Resource", Name = "_lastUpdated", Type = SearchParamType.Date, Expression = "Resource.meta.lastUpdated", Path = new string[] { "Resource.meta.lastUpdated" } }
            , new SearchParamDefinition { Resource = "Resource", Name = "_profile", Type = SearchParamType.Uri, Expression = "Resource.meta.profile", Path = new string[] { "Resource.meta.profile" } }
            , new SearchParamDefinition { Resource = "Resource", Name = "_security", Type = SearchParamType.Token, Expression = "Resource.meta.security", Path = new string[] { "Resource.meta.security" } }
            , new SearchParamDefinition { Resource = "Resource", Name = "_tag", Type = SearchParamType.Token, Expression = "Resource.meta.tag", Path = new string[] { "Resource.meta.tag" } }
        };
        var genericSearchParameters = genericSearchParamDefinitions.Select(spd => createSearchParameterFromSearchParamDefinition(spd));

        _searchParameters.AddRange(genericSearchParameters.Except(_searchParameters));
        //We have no control over the incoming list of searchParameters (in the constructor), so these generic parameters may or may not be in there.
        //So we apply the Except operation to make sure these parameters are not added twice.
    }

    private SearchParameter createSearchParameterFromSearchParamDefinition(SearchParamDefinition def)
    {
        var result = new ComparableSearchParameter();
        result.Name = def.Name;
        result.Code = def.Name; //CK: SearchParamDefinition has no Code, but in all current SearchParameter resources, name and code are equal.
        result.Base = new List<ResourceType?> { GetResourceTypeForResourceName(def.Resource) };
        result.Type = def.Type;
        result.Target = def.Target != null ? def.Target.ToList().Cast<ResourceType?>() : new List<ResourceType?>();
        result.Description = def.Description;
        // NOTE: This is a fix to handle an issue in firely-net-sdk
        // where the expression 'ConceptMap.source as uri' returns
        // a string instead of uri.
        // FIXME: On a longer term we should refactor the
        // SearchParameter in-memory cache so we can more elegantly
        // swap out a SearchParameter
        if (def.Resource == ResourceType.ConceptMap.GetLiteral())
        {
            if (def.Name == "source-uri")
            {
                result.Expression = "ConceptMap.source.as(uri)";
            }
            else if (def.Name == "target-uri")
            {
                result.Expression = "ConceptMap.target.as(uri)";
            }
            else
            {
                result.Expression = def.Expression;
            }
        }
        else
        {
            result.Expression = def.Expression;
        }
        //Strip off the [x], for example in Condition.onset[x].
        result.SetPropertyPath(def.Path?.Select(p => p.Replace("[x]", "")).ToArray());

        //Watch out: SearchParameter is not very good yet with Composite parameters.
        //Therefore we include a reference to the original SearchParamDefinition :-)
        result.SetOriginalDefinition(def);

        return result;
    }

    private class ComparableSearchParameter : SearchParameter, IEquatable<ComparableSearchParameter>
    {
        public bool Equals(ComparableSearchParameter other)
        {
            return string.Equals(Name, other.Name) &&
                   string.Equals(Code, other.Code) &&
                   Equals(Base, other.Base) &&
                   Equals(Type, other.Type) &&
                   string.Equals(Description, other.Description) &&
                   string.Equals(Xpath, other.Xpath);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ComparableSearchParameter)obj);
        }

        public override int GetHashCode()
        {
            var hashCode = (Name != null ? Name.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Code != null ? Code.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Base != null ? Base.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Type != null ? Type.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Xpath != null ? Xpath.GetHashCode() : 0);
            return hashCode;
        }
    }

    public List<SearchParameter> SearchParameters
    {
        get
        {
            return _searchParameters;
        }
    }

    public string GetResourceNameForType(Type type)
    {
        if (_resourceTypeToResourceTypeName != null)
        {
            return _resourceTypeToResourceTypeName[type];
        }
        return GetFhirTypeNameForType(type);

    }

    public Type GetTypeForResourceName(string name)
    {
        return GetTypeForFhirType(name);
    }

    public ResourceType GetResourceTypeForResourceName(string name)
    {
        return (ResourceType)Enum.Parse(typeof(ResourceType), name, true);
    }

    public string GetResourceNameForResourceType(ResourceType type)
    {
        return Enum.GetName(typeof(ResourceType), type);
    }

    public IEnumerable<SearchParameter> FindSearchParameters(Type resourceType)
    {
        return FindSearchParameters(GetResourceNameForType(resourceType));
    }

    public IEnumerable<SearchParameter> FindSearchParameters(string resourceName)
    {
        //return SearchParameters.Where(sp => sp.Base == GetResourceTypeForResourceName(resourceName) || sp.Base == ResourceType.Resource);
        return SearchParameters.Where(sp => sp.Base.Contains(GetResourceTypeForResourceName(resourceName)) || sp.Base.Any(b => b == ResourceType.Resource));
    }
    public IEnumerable<SearchParameter> FindSearchParameters(ResourceType resourceType)
    {
        return FindSearchParameters(GetResourceNameForResourceType(resourceType));
    }

    public SearchParameter FindSearchParameter(ResourceType resourceType, string parameterName)
    {
        return FindSearchParameter(GetResourceNameForResourceType(resourceType), parameterName);
    }

    public SearchParameter FindSearchParameter(Type resourceType, string parameterName)
    {
        return FindSearchParameter(GetResourceNameForType(resourceType), parameterName);
    }

    public SearchParameter FindSearchParameter(string resourceName, string parameterName)
    {
        return FindSearchParameters(resourceName).Where(sp => sp.Name == parameterName).FirstOrDefault();
    }

    public string GetLiteralForEnum(Enum value)
    {
        return value.GetLiteral();
    }

    private readonly List<CompartmentInfo> _compartments = new List<CompartmentInfo>();
    private void LoadCompartments()
    {
        // FIXME: This might be better resolved through a CompartmentDefinition.
        var searchParameters = SearchParameters.Where(searchParameter =>
            searchParameter.Type == SearchParamType.Reference
            && searchParameter.Target.Contains(ResourceType.Patient)
            && !searchParameter.Base.Any(IsDefinitionResourceType)
            && searchParameter.Name != "subject");
        var reverseIncludes = new List<string>();
        foreach (SearchParameter searchParameter in searchParameters)
        {
            foreach (ResourceType? resourceType in searchParameter.Base)
            {
                if (!resourceType.HasValue)
                    continue;

                reverseIncludes.Add($"{resourceType.GetLiteral()}:{searchParameter.Name}");
            }
        }

        var patientCompartmentInfo = new CompartmentInfo(ResourceType.Patient);
        patientCompartmentInfo.AddReverseIncludes(reverseIncludes);
        _compartments.Add(patientCompartmentInfo);
    }

    private bool IsDefinitionResourceType(ResourceType? resourceType)
    {
        return resourceType.HasValue && DefinitionResourceTypes().Contains(resourceType.Value);
    }

    private static IEnumerable<ResourceType> DefinitionResourceTypes()
    {
        return new[]
        {
            ResourceType.ActivityDefinition,
            ResourceType.DeviceDefinition,
            ResourceType.CompartmentDefinition,
            ResourceType.EventDefinition,
            ResourceType.GraphDefinition,
            ResourceType.MessageDefinition,
            ResourceType.ObservationDefinition,
            ResourceType.OperationDefinition,
            ResourceType.PlanDefinition,
            ResourceType.ResearchDefinition,
            ResourceType.SpecimenDefinition,
            ResourceType.StructureDefinition,
            ResourceType.ChargeItemDefinition,
            ResourceType.ResearchElementDefinition
        };
    }

    public CompartmentInfo FindCompartmentInfo(ResourceType resourceType)
    {
        return _compartments.Where(ci => ci.ResourceType == resourceType).FirstOrDefault();
    }

    public CompartmentInfo FindCompartmentInfo(string resourceType)
    {
        return FindCompartmentInfo(GetResourceTypeForResourceName(resourceType));
    }
}
