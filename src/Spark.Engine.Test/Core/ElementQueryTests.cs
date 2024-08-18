/* 
 * Copyright (c) 2015-2018, Firely (info@fire.ly)
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Engine.Core;
using Hl7.Fhir.Model;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Engine.Test.Core
{
    [TestClass]
    public class ElementQueryTests
    {
        [TestMethod]
        public void TestVisitOnePathZeroMatch()
        {
            ElementQuery sut = new ElementQuery("Patient.name");

            Patient testPatient = new Patient();
            var result = new List<Object>() ;

            sut.Visit(testPatient, fd => result.Add(fd));

            Assert.AreEqual(testPatient.Name.Count, result.Where(ob => ob != null).Count());
        }

        [TestMethod]
        public void TestVisitOnePathOneMatch()
        {
            ElementQuery sut = new ElementQuery("Patient.name");

            Patient testPatient = new Patient();
            var hn = new HumanName().WithGiven("Sjors").AndFamily("Jansen");
            testPatient.Name = new List<HumanName> { hn };

            var result = new List<Object>();

            sut.Visit(testPatient, fd => result.Add(fd));

            Assert.AreEqual(testPatient.Name.Count, result.Where(ob => ob != null).Count());
            Assert.IsTrue(result.Contains(hn));
        }

        [TestMethod]
        public void TestVisitOnePathTwoMatches()
        {
            ElementQuery sut = new ElementQuery("Patient.name");

            Patient testPatient = new Patient();
            var hn1 = new HumanName().WithGiven("A").AndFamily("B");
            var hn2 = new HumanName().WithGiven("Y").AndFamily("Z");
            testPatient.Name = new List<HumanName> { hn1, hn2 };

            var result = new List<Object>();

            sut.Visit(testPatient, fd => result.Add(fd));

            Assert.AreEqual(testPatient.Name.Count, result.Where(ob => ob != null).Count());
            Assert.IsTrue(result.Contains(hn1));
            Assert.IsTrue(result.Contains(hn2));
        }
    }
}
