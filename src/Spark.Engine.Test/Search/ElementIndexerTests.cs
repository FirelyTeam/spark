using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Engine.Core;
using Spark.Engine.Model;
using Spark.Engine.Search;
using Spark.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Engine.Search.Tests
{
    [TestClass()]
    public class ElementIndexerTests
    {
        private ElementIndexer sut;

        [TestInitialize]
        public void InitializeTest()
        {
            var fhirModel = new FhirModel();
            sut = new ElementIndexer(fhirModel);
        }

        [TestMethod()]
        public void ElementIndexerTest()
        {
            Assert.IsNotNull(sut);
            Assert.IsInstanceOfType(sut, typeof(ElementIndexer));
        }

        [TestMethod()]
        public void ElementToExpressionsTest()
        {
            var input = new Annotation();
            input.Text = "Text of the annotation";
            var result = sut.ToExpressions(input);
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOfType(result.First(), typeof(StringValue));
            Assert.AreEqual(input.ToString(), ((StringValue)result.First()).Value);
        }

        [TestMethod()]
        public void FhirDecimalToExpressionsTest()
        {
            var input = new FhirDecimal(1081.54M);
            var result = sut.ToExpressions(input);
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOfType(result.First(), typeof(NumberValue));
            Assert.AreEqual(1081.54M, ((NumberValue)result.First()).Value);
        }

        [TestMethod()]
        public void FhirDateTimeToExpressionsTest()
        {
            var input = new FhirDateTime(2015, 3, 14);
            var result = sut.ToExpressions(input);
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOfType(result.First(), typeof(CompositeValue));
            var comp = result.First() as CompositeValue;
            Assert.AreEqual(new DateValue("2015-03-14T00:00:00"), ((DateValue)comp.Components.Where(c => c is IndexValue).Where(c => (c as IndexValue).Name == "start").Select(c => (c as IndexValue).Values.First())));
        }

        [TestMethod()]
        public void ToExpressionsTest3()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ToExpressionsTest4()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ToExpressionsTest5()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ToExpressionsTest6()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ToExpressionsTest7()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ToExpressionsTest8()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ToExpressionsTest9()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ToExpressionsTest10()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ToExpressionsTest11()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ToExpressionsTest12()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ToExpressionsTest13()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ToExpressionsTest14()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ToExpressionsTest15()
        {
            Assert.Fail();
        }
    }
}