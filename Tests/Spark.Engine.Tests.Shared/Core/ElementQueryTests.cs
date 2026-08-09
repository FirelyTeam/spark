/* 
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using Xunit;
using Spark.Engine.Core;
using Hl7.Fhir.Model;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Engine.Tests.Core;

public class ElementQueryTests
{
    [Fact]
    public void TestVisitOnePathZeroMatch()
    {
        ElementQuery sut = new(new FhirModel(), "Patient.name");

        Patient testPatient = new Patient();
        var result = new List<Object>() ;

        sut.Visit(testPatient, fd => result.Add(fd));

        Assert.Equal(testPatient.Name.Count, result.Count(ob => ob != null));
    }

    [Fact]
    public void TestVisitOnePathOneMatch()
    {
        ElementQuery sut = new(new FhirModel(), "Patient.name");

        Patient testPatient = new Patient();
        var hn = new HumanName().WithGiven("Sjors").AndFamily("Jansen");
        testPatient.Name = new List<HumanName> { hn };

        var result = new List<Object>();

        sut.Visit(testPatient, fd => result.Add(fd));

        Assert.Equal(testPatient.Name.Count, result.Count(ob => ob != null));
        Assert.Contains(hn, result);
    }

    [Fact]
    public void TestVisitOnePathTwoMatches()
    {
        ElementQuery sut = new(new FhirModel(), "Patient.name");

        Patient testPatient = new Patient();
        var hn1 = new HumanName().WithGiven("A").AndFamily("B");
        var hn2 = new HumanName().WithGiven("Y").AndFamily("Z");
        testPatient.Name = new List<HumanName> { hn1, hn2 };

        var result = new List<Object>();

        sut.Visit(testPatient, fd => result.Add(fd));

        Assert.Equal(testPatient.Name.Count, result.Where(ob => ob != null).Count());
        Assert.Contains(hn1, result);
        Assert.Contains(hn2, result);
    }
}
