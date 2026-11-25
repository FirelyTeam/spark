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
using Spark.Engine.Extensions;

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
    /// The elements to serialize: the ones asked for, plus the mandatory and modifier ones, and the resource is tagged
    /// as SUBSETTED. Null when _elements does not apply, and the resource is serialized as it is.
    /// </summary>
    internal static string[] ForElements(Resource resource, SummaryType summary, string[] elements)
    {
        // _summary wins over _elements, as it does in the serializer.
        if (resource == null || resource is Bundle || summary != SummaryType.False || elements == null || elements.Length == 0)
            return null;

        // The resource is read for this response alone, so it is tagged in place rather than copied.
        resource.MarkSubsetted();

        return [.. elements.Union(AlwaysIncluded).Union(GetMandatoryAndModifierElements(resource.GetType()))];
    }

    /// <summary>The mandatory elements of the type, and the modifier ones that change what it means, so a subset has to carry them.</summary>
    private static IEnumerable<string> GetMandatoryAndModifierElements(Type resourceType)
    {
        ClassMapping classMapping = ModelInfo.ModelInspector.FindOrImportClassMapping(resourceType);

        return classMapping == null
            ? []
            : classMapping.PropertyMappings
                .Where(property => property.IsModifier || property.IsMandatoryElement)
                .Select(property => property.Name);
    }
}
