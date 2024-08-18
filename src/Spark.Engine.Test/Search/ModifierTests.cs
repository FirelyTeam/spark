/* 
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Engine.Search.Model;

namespace Spark.Engine.Test.Search
{
    [TestClass]
    public class ModifierTests
    {
        [TestMethod]
        public void TestActualModifierConstructorWithMissingModifiers()
        {
            var am = new ActualModifier("missing");
            Assert.AreEqual(Modifier.MISSING, am.Modifier);
            Assert.AreEqual("missing", am.RawModifier);
            Assert.IsNull(am.ModifierType);
            Assert.IsTrue(am.Missing.Value);
            Assert.AreEqual("missing=true", am.ToString());

            am = new ActualModifier("missing=false");
            Assert.AreEqual(Modifier.MISSING, am.Modifier);
            Assert.AreEqual("missing=false", am.RawModifier);
            Assert.IsNull(am.ModifierType);
            Assert.IsFalse(am.Missing.Value);
            Assert.AreEqual("missing=false", am.ToString());
        }

        [TestMethod]
        public void TestActualModifierConstructorWithValidTypeModifier()
        { 
            var am = new ActualModifier("Patient");
            Assert.AreEqual(Modifier.TYPE, am.Modifier);
            Assert.AreEqual("Patient", am.RawModifier);
            Assert.AreEqual(typeof(Patient), am.ModifierType);
            Assert.AreEqual("Patient", am.ToString());
        }

        [TestMethod]
        public void TestActualModifierConstructorWithInvalidModifier()
        {
            var am = new ActualModifier("blabla");
            Assert.AreEqual(Modifier.UNKNOWN, am.Modifier);
            Assert.AreEqual("blabla", am.RawModifier);
            Assert.IsNull(am.ModifierType);
            Assert.AreEqual(null, am.ToString());
        }
    }
}
