/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Spark.Engine.Core;

namespace Spark.Engine.Tests.Core;

public partial class CapabilityStatementBuilderTests
{
    private static CapabilityStatementBuilder WithVersionSpecificFhirVersion(CapabilityStatementBuilder builder)
        => builder.WithFhirVersion(FHIRVersion.N4_0_1);

    private static bool IsPatientResource(CapabilityStatement.ResourceComponent resource)
        => resource.Type == "Patient";

    private static bool IsDocumentReferenceResource(CapabilityStatement.ResourceComponent resource)
        => resource.Type == "DocumentReference";
}
