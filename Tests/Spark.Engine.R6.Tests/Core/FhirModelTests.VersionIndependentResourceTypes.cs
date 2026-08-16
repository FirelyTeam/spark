/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using System;
using System.Linq;
using Xunit;

namespace Spark.Engine.Tests;

public class FhirModelVersionIndependentResourceTypesTests
{
    [Fact]
    public void SupportedResources_AreRepresentedByVersionIndependentResourceTypesAll()
    {
        var versionIndependentResourceTypes = Enum
            .GetNames<VersionIndependentResourceTypesAll>()
            .ToHashSet(StringComparer.Ordinal);

        var missingResourceTypes = ModelInfo.SupportedResources
            .Where(resourceType => !versionIndependentResourceTypes.Contains(resourceType))
            .OrderBy(resourceType => resourceType)
            .ToArray();

        Assert.Equal(
            [
                "ClinicalAssessment",
                "DeviceAlert",
                "InsuranceProduct",
                "MolecularDefinition",
                "PersonalRelationship"
            ],
            missingResourceTypes);
    }
}
