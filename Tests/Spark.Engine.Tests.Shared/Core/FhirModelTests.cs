/* 
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Engine.Core;
using System.Linq;
using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;
using Xunit;

namespace Spark.Engine.Tests.Core;

public class FhirModelTests
{
    [Fact]
    public void TestCompartments()
    {
        FhirModel model = new();
        var actual = model.FindCompartmentInfo(ResourceType.Patient.GetLiteral());

        Assert.NotNull(actual);
        Assert.True(actual.ReverseIncludes.Any());
    }
}
