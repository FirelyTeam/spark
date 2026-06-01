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
    private bool ContainsPatientResource(ResourceType?[] resourceTypes)
        => resourceTypes.Contains(ResourceType.Patient);

    private bool ContainsObservationResource(ResourceType?[] resourceTypes)
        => resourceTypes.Contains(ResourceType.Observation);
    
    private static bool IsPatientResource(CapabilityStatement.ResourceComponent resource)
        => resource.Type == ResourceType.Patient;

    private static bool ContainsOperationDefinition(string expected, IDictionary<ResourceReference, string> operations)
        => operations.Any(operation => operation.Key.Reference != null && operation.Key.Reference.Contains(expected));
}
