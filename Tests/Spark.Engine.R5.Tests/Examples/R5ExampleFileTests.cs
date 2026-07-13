/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System;
using System.IO;
using Xunit;

namespace Spark.Engine.Tests.Examples;

public class R5ExampleFileTests
{
    [Theory]
    [InlineData("patient-example.json", typeof(Patient), "example")]
    [InlineData("observation-example-bloodpressure.json", typeof(Observation), "blood-pressure")]
    [InlineData("medicationrequest0301.json", typeof(MedicationRequest), "medrx0301")]
    public void CanParseR5JsonExamples(string fileName, Type expectedType, string expectedId)
    {
        var json = File.ReadAllText(Path.Combine("Examples", fileName));

        var deserializer = new FhirJsonDeserializer();
        var resource = deserializer.Deserialize<Resource>(json);

        Assert.IsType(expectedType, resource);
        Assert.Equal(expectedId, resource.Id);
    }
}
