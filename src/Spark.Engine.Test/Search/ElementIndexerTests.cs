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

        private void CheckPeriod(List<Expression> result, string start, string end)
        {
            var nrOfComponents = 0;
            if (!String.IsNullOrWhiteSpace(start)) nrOfComponents++;
            if (!String.IsNullOrWhiteSpace(end)) nrOfComponents++;
            
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOfType(result.First(), typeof(CompositeValue));
            var comp = result.First() as CompositeValue;
            Assert.AreEqual(nrOfComponents, comp.Components.Count());

            var currentComponent = 0;
            if (!String.IsNullOrWhiteSpace(start))
            {
                Assert.IsInstanceOfType(comp.Components[currentComponent], typeof(IndexValue));
                var ixValue = comp.Components[currentComponent] as IndexValue;
                Assert.AreEqual("start", ixValue.Name);
                Assert.AreEqual(1, ixValue.Values.Count());
                Assert.IsInstanceOfType(ixValue.Values[0], typeof(DateTimeValue));
                var dtValue = ixValue.Values[0] as DateTimeValue;
                Assert.AreEqual(new DateTimeValue(start).Value, dtValue.Value);
                currentComponent++;
            }

            if (!String.IsNullOrWhiteSpace(end))
            {
                Assert.IsInstanceOfType(comp.Components[currentComponent], typeof(IndexValue));
                var ixValue = comp.Components[currentComponent] as IndexValue;
                Assert.AreEqual("end", ixValue.Name);
                Assert.AreEqual(1, ixValue.Values.Count());
                Assert.IsInstanceOfType(ixValue.Values[0], typeof(DateTimeValue));
                var dtValue = ixValue.Values[0] as DateTimeValue;
                Assert.AreEqual(new DateTimeValue(end).Value, dtValue.Value);
            }
        }
        [TestMethod()]
        public void FhirDateTimeToExpressionsTest()
        {
            var input = new FhirDateTime(2015, 3, 14);
            var result = sut.ToExpressions(input);
            CheckPeriod(result, "2015-03-14T00:00:00+01:00", "2015-03-15T00:00:00+01:00");
        }

        [TestMethod()]
        public void PeriodWithStartAndEndToExpressionsTest()
        {
            var input = new Period();
            input.StartElement = new FhirDateTime("2015-02");
            input.EndElement = new FhirDateTime("2015-03");
            var result = sut.ToExpressions(input);
            CheckPeriod(result, "2015-02-01T00:00:00+01:00", "2015-04-01T00:00:00+01:00");
        }

        [TestMethod()]
        public void PeriodWithJustStartToExpressionsTest()
        {
            var input = new Period();
            input.StartElement = new FhirDateTime("2015-02");
            var result = sut.ToExpressions(input);
            CheckPeriod(result, "2015-02-01T00:00:00+01:00", null);
        }

        [TestMethod()]
        public void PeriodWithJustEndToExpressionsTest()
        {
            var input = new Period();
            input.EndElement = new FhirDateTime("2015-03");
            var result = sut.ToExpressions(input);
            CheckPeriod(result, null, "2015-04-01T00:00:00+01:00");
        }

        [TestMethod()]
        public void CodingToExpressionsTest()
        {
            var input = new Coding();
            input.CodeElement = new Code("bla");
            input.SystemElement = new FhirUri("http://bla.com");
            input.DisplayElement = new FhirString("bla display");
            var result = sut.ToExpressions(input);

            Assert.AreEqual(1, result.Count());
            Assert.IsInstanceOfType(result[0], typeof(CompositeValue));
            var comp = result[0] as CompositeValue;

            Assert.AreEqual(3, comp.Components.Count());
            foreach (var c in comp.Components)
            {
                Assert.IsInstanceOfType(c, typeof(IndexValue));
            }

            var codeIV = (IndexValue)comp.Components.Where(c => (c as IndexValue).Name == "code").FirstOrDefault();
            Assert.IsNotNull(codeIV);
            Assert.AreEqual(1, codeIV.Values.Count());
            Assert.IsInstanceOfType(codeIV.Values[0], typeof(StringValue));
            var codeSV = (StringValue)codeIV.Values[0];
            Assert.AreEqual("bla", codeSV.Value);

            var systemIV = (IndexValue)comp.Components.Where(c => (c as IndexValue).Name == "system").FirstOrDefault();
            Assert.IsNotNull(systemIV);
            Assert.AreEqual(1, systemIV.Values.Count());
            Assert.IsInstanceOfType(systemIV.Values[0], typeof(StringValue));
            var systemSV = (StringValue)systemIV.Values[0];
            Assert.AreEqual("http://bla.com", systemSV.Value);

            var textIV = (IndexValue)comp.Components.Where(c => (c as IndexValue).Name == "text").FirstOrDefault();
            Assert.IsNotNull(textIV);
            Assert.AreEqual(1, textIV.Values.Count());
            Assert.IsInstanceOfType(textIV.Values[0], typeof(StringValue));
            var textSV = (StringValue)textIV.Values[0];
            Assert.AreEqual("bla", textSV.Value);
        }

        [TestMethod()]
        public void CodeableConceptToExpressionsTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void IdentifierToExpressionsTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ContactPointToExpressionsTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void FhirBooleanToExpressionsTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ResourceReferenceToExpressionsTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void AddressToExpressionsTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void HumanNameToExpressionsTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void QuantityToExpressionsTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void CodeToExpressionsTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void FhirStringToExpressionsTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ListOfElementsToExpressionsTest()
        {
            Assert.Fail();
        }
    }
}