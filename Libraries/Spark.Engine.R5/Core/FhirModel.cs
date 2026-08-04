/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2017-2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;

namespace Spark.Engine.Core;

public class FhirModel : FhirModelBase
{
    // This constructor is only supposed to be accessed by tests and is therefore marked as internal.
    internal FhirModel(Dictionary<Type, string> resourceTypeToResourceTypeNameMapping, IEnumerable<SearchParamDefinition> searchParameters)
        : base(resourceTypeToResourceTypeNameMapping, searchParameters) { }

    public FhirModel() : base(ModelInfo.SearchParameters) { }

    public FhirModel(IEnumerable<SearchParamDefinition> searchParameters) : base(searchParameters) { }

    public override IReadOnlyList<string> SupportedResources => ModelInfo.SupportedResources;

    public override string FhirRelease => ModelInfo.Version;

    public override ModelInspector GetModelInspector() => ModelInfo.ModelInspector;

    public override Type GetTypeForFhirType(string typeName) => ModelInfo.GetTypeForFhirType(typeName);

    public override string GetFhirTypeNameForType(Type type) => ModelInfo.GetFhirTypeNameForType(type);

    protected override SearchParamDefinition[] GetGenericSearchParamDefinitions()
    {
        return [
            new()
            {
                Resource = "Resource",
                Name = "_id",
                Type = SearchParamType.Token,
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
                Name = "_tag",
                Type = SearchParamType.Token,
                Expression = "Resource.meta.tag",
                Path = ["Resource.meta.tag"]
            },
            new()
            {
                Resource = "Resource",
                Name = "_profile",
                Type = SearchParamType.Reference,
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
                Name = "_source",
                Type = SearchParamType.Uri,
                Expression = "Resource.meta.source",
                Path = ["Resource.meta.source"]
            }
        ];
    }
}
