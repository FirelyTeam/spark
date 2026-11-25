/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.Linq;
using Hl7.Fhir.Model;

namespace Spark.Engine.Extensions;

internal static class ResourceExtensions
{
    private const string SubsettedSystem = "http://terminology.hl7.org/CodeSystem/v3-ObservationValue";
    private const string SubsettedCode = "SUBSETTED";

    /// <summary>
    /// Tags the resource as SUBSETTED, so that nobody takes it for the whole resource.
    /// Only the resource itself is tagged, not the resources it contains.
    /// </summary>
    internal static void MarkSubsetted(this Resource resource)
    {
        resource.Meta ??= new Meta();
        if (resource.Meta.Tag.Any(tag => tag.System == SubsettedSystem && tag.Code == SubsettedCode))
            return;

        resource.Meta.Tag.Add(new Coding
        {
            System = SubsettedSystem,
            Code = SubsettedCode,
            Display = "subsetted",
        });
    }
}
