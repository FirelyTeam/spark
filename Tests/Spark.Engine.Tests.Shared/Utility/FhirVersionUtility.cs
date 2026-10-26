/* 
 * Copyright (c) 2020-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;
using NuGet.Versioning;

namespace Spark.Engine.Tests.Utility;

internal class FhirVersionUtility
{
    public const string VERSION_R2 = "1.0";
    public const string VERSION_R3 = "3.0";
    public const string VERSION_R4 = "4.0";
    public const string VERSION_R4B = "4.3";
    public const string VERSION_R5 = "5.0";
    public const string VERSION_R6 = "6.0";

    public static FhirVersionMoniker GetFhirVersionMoniker()
    {
        FhirVersionMoniker? fhirVersion = default;
        if (SemanticVersion.TryParse(ModelInfo.Version, out SemanticVersion semanticVersion))
        {
            fhirVersion = EnumUtility.ParseLiteral<FhirVersionMoniker>($"{semanticVersion.Major}.{semanticVersion.Minor}");
        }

        return fhirVersion ?? FhirVersionMoniker.None;
    }
}
