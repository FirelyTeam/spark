/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Spark.Engine.Extensions;
using Spark.Engine.Utility;

namespace Spark.Engine.Formatters;

/// <summary>
/// Cuts a single resource down to the elements asked for with _elements, for the interactions that return one resource,
/// like read and vread. Search does this in the store, so its bundles are left alone here.
/// </summary>
internal static class ElementsSubset
{
    /// <summary>The elements a subsetted resource keeps even when nobody asks for them: its identity, and where the SUBSETTED tag goes.</summary>
    private static readonly string[] AlwaysIncluded = ["id", "meta"];

    /// <summary>
    /// The resource tagged as SUBSETTED, and the filter that serializes only the elements asked for, plus the mandatory
    /// and modifier ones. Null when _elements does not apply, and the resource is serialized as it is.
    /// </summary>
    internal static (Resource Resource, Func<SerializationFilter> Filter)? ForElements(Resource resource, SummaryType summary, string[] elements)
    {
        // _summary wins over _elements, as it does in the Firely SDK.
        if (resource == null || resource is Bundle || summary != SummaryType.False || elements == null || elements.Length == 0)
            return null;

        string[] includeNames = [.. elements.Union(AlwaysIncluded).Union(GetModifierElements(resource.GetType()))];

        // The resource is read for this response alone, so it is tagged in place rather than copied.
        resource.MarkSubsetted();

        // The filter keeps mandatory elements on its own. The Firely SDK cannot tell modifier elements yet (its
        // IncludeIsModifier throws NotSupportedException), so those are added to the names above instead.
        return (resource, () => SerializationFilter.ForElements(includeNames, includeMandatory: true));
    }

    /// <summary>The modifier elements of the type: the ones that change what the resource means, so a subset has to carry them.</summary>
    private static IEnumerable<string> GetModifierElements(Type resourceType)
    {
        if (StaticReferenceToFhirModel.FhirModel == null)
            throw new InvalidOperationException($"{nameof(StaticReferenceToFhirModel)}.FhirModel is not set.");

        ClassMapping classMapping = StaticReferenceToFhirModel.FhirModel.GetModelInspector().FindOrImportClassMapping(resourceType);

        return classMapping == null
            ? []
            : classMapping.PropertyMappings
                .Where(property => property.IsModifier)
                .Select(property => property.Name);
    }
}
