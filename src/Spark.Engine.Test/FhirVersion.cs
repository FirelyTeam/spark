/* 
 * Copyright (c) 2020-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using Hl7.Fhir.Utility;
using Spark.Engine.Test.Utility;

namespace Spark.Engine.Test
{
    public enum FhirVersionMoniker
    {
        [EnumLiteral("")]
        None = 0,
        [EnumLiteral(FhirVersionUtility.VERSION_R2)]
        R2 = 2,
        [EnumLiteral(FhirVersionUtility.VERSION_R3)]
        R3 = 3,
        [EnumLiteral(FhirVersionUtility.VERSION_R4)]
        R4 = 4,
        [EnumLiteral(FhirVersionUtility.VERSION_R5)]
        R5 = 5,
    }
}