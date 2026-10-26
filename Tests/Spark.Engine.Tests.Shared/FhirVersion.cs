/* 
 * Copyright (c) 2020-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Utility;
using Spark.Engine.Tests.Utility;

namespace Spark.Engine.Tests;

public enum FhirVersionMoniker
{
    [EnumLiteral("")]
    None = 0,
    [EnumLiteral(FhirVersionUtility.VersionR2)]
    R2 = 2,
    [EnumLiteral(FhirVersionUtility.VersionR3)]
    R3 = 3,
    [EnumLiteral(FhirVersionUtility.VersionR4)]
    R4 = 4,
    [EnumLiteral(FhirVersionUtility.VersionR4B)]
    R4B = 43,
    [EnumLiteral(FhirVersionUtility.VersionR5)]
    R5 = 5,
    [EnumLiteral(FhirVersionUtility.VersionR6)]
    R6 = 6,
}
