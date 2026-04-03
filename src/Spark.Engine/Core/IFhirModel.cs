/* 
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Engine.Model;
using System;
using System.Collections.Generic;

namespace Spark.Engine.Core;

public interface IFhirModel
{
    IReadOnlyList<SearchParameter> SearchParameters { get; }

    IReadOnlyList<string> SupportedResources { get; }

    /// <summary>
    /// Maps any FHIR type name (resource, primitive, complex) to its C# type.
    /// </summary>
    Type GetTypeForFhirType(string typeName);

    /// <summary>
    /// Maps a C# type to the FHIR type name used in paths and expressions.
    /// </summary>
    string GetFhirTypeNameForType(Type type);

    /// <summary>
    /// "Patient" -> Hl7.Fhir.Model.Patient
    /// </summary>
    /// <param name="name"></param>
    /// <returns>type belonging to the name, if known (otherwise null)</returns>
    Type GetTypeForResourceName(string name);

    /// <summary>
    /// Hl7.Fhir.Model.Patient -> "Patient"
    /// </summary>
    /// <param name="type"></param>
    /// <returns>name of the type as it is used in the REST interface, if known (otherwise null)</returns>
    string GetResourceNameForType(Type type);

    IEnumerable<SearchParameter> FindSearchParameters(Type resourceType);

    IEnumerable<SearchParameter> FindSearchParameters(string resourceType);

    SearchParameter FindSearchParameter(Type resourceType, string parameterName);

    SearchParameter FindSearchParameter(string resourceTypeName, string parameterName);

    /// <summary>
    /// Get the string value for an enum as specified in the EnumLiteral attribute.
    /// </summary>
    /// <param name="enumType"></param>
    /// <param name="value"></param>
    /// <returns>String for the enum value if found, otherwise null</returns>
    string GetLiteralForEnum(Enum value);

    CompartmentInfo FindCompartmentInfo(string resourceType);
}
