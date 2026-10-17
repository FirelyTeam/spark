/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using System;
using System.Net;
using Xunit;

namespace Spark.Engine.Tests.Extensions;

public class OperationOutcomeSerializationTests
{
    [Fact]
    public void AddAllInnerErrors_Result_Can_Be_Parsed_In_Strict_Mode()
    {
        var exception = new SparkException(HttpStatusCode.BadRequest, "Body parsing failed", new InvalidOperationException("Inner error"));
        var outcome = new OperationOutcome().AddAllInnerErrors(exception);

        var json = new FhirJsonSerializer().SerializeToString(outcome);
        var deserializer = new FhirJsonDeserializer(new DeserializerSettings().UsingMode(DeserializationMode.Strict));

        var parsed = deserializer.Deserialize<OperationOutcome>(json);

        Assert.Equal(2, parsed.Issue.Count);
    }
}
