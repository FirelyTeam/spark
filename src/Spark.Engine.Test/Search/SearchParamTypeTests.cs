using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Engine.Search.Model;

namespace Spark.Engine.Test.Search
{
    [TestClass]
    public class SearchParamTypeTests

    {
        [TestMethod]
        public void TestModifierIsAllowed()
        {
            var sptString = new SearchParamTypeString();
            var sptReference = new SearchParamTypeReference();

            Assert.IsTrue(sptString.ModifierIsAllowed(new ActualModifier("exact")));
            Assert.IsFalse(sptReference.ModifierIsAllowed(new ActualModifier("exact")));
            Assert.IsTrue(sptReference.ModifierIsAllowed(new ActualModifier("Patient")));
        }
    }
}
