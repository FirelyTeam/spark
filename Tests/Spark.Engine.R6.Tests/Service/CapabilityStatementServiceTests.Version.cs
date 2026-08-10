/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Engine.Tests.Service;

public partial class CapabilityStatementServiceTests
{
    private bool ContainsPatientResource(string[] resourceTypes)
        => resourceTypes.Contains("Patient");

    private bool ContainsObservationResource(string[] resourceTypes)
        => resourceTypes.Contains("Observation");
    
    private static bool IsPatientResource(CapabilityStatement.ResourceComponent resource)
        => resource.Type == "Patient";
    
    private static bool ContainsOperationDefinition(string expected, IDictionary<string, string> operations)
        => operations.Any(operation => operation.Key.Contains(expected));
}
