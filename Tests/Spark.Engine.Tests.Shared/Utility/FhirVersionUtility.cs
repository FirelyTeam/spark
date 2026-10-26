/* 
 * Copyright (c) 2020-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;
using NuGet.Versioning;

namespace Spark.Engine.Tests.Utility;

internal static class FhirVersionUtility
{
    public const string VersionR2 = "1.0";
    public const string VersionR3 = "3.0";
    public const string VersionR4 = "4.0";
    public const string VersionR4B = "4.3";
    public const string VersionR5 = "5.0";
    public const string VersionR6 = "6.0";

    public static FhirVersionMoniker GetFhirVersionMoniker()
    {
        FhirVersionMoniker? fhirVersion = null;
        if (SemanticVersion.TryParse(ModelInfo.Version, out SemanticVersion semanticVersion))
        {
            fhirVersion = EnumUtility.ParseLiteral<FhirVersionMoniker>($"{semanticVersion.Major}.{semanticVersion.Minor}");
        }

        return fhirVersion ?? FhirVersionMoniker.None;
    }
}
