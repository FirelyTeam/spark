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

    protected override IEnumerable<VersionIndependentResourceTypesAll> DefinitionResourceTypes() =>
    [
        VersionIndependentResourceTypesAll.ActivityDefinition,
        VersionIndependentResourceTypesAll.ChargeItemDefinition,
        VersionIndependentResourceTypesAll.CompartmentDefinition,
        VersionIndependentResourceTypesAll.DeviceDefinition,
        VersionIndependentResourceTypesAll.EventDefinition,
        VersionIndependentResourceTypesAll.GraphDefinition,
        VersionIndependentResourceTypesAll.MessageDefinition,
        VersionIndependentResourceTypesAll.ObservationDefinition,
        VersionIndependentResourceTypesAll.OperationDefinition,
        VersionIndependentResourceTypesAll.PlanDefinition,
        VersionIndependentResourceTypesAll.ResearchElementDefinition,
        VersionIndependentResourceTypesAll.ResearchDefinition,
        VersionIndependentResourceTypesAll.SpecimenDefinition,
        VersionIndependentResourceTypesAll.StructureDefinition,
    ];
}
