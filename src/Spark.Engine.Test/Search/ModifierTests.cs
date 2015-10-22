using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Engine.Search.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Engine.Test.Search
{
    [TestClass]
    public class ModifierTests
    {
        [TestMethod]
        public void TestActualModifierConstructor()
        {
            var am = new ActualModifier("missing");
            Assert.AreEqual(Modifier.MISSING, am.Modifier);
            Assert.AreEqual("missing", am.RawModifier);
            Assert.IsNull(am.ModifierType);
            Assert.AreEqual("missing=true", am.ToString());

            am = new ActualModifier("[Patient]");
            Assert.AreEqual(Modifier.TYPE, am.Modifier);
            Assert.AreEqual("[Patient]", am.RawModifier);
            Assert.AreEqual(typeof(Patient), am.ModifierType);
            Assert.AreEqual("[Patient]", am.ToString());
        }
    }
}
