using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Engine.Core;
using Spark.Engine.Logging;
using Spark.Engine.Model;
using Spark.Search;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;

namespace Spark.Engine.Search.Tests
{
    [TestClass()]
    public class ElementIndexerTests
    {
        private ElementIndexer _sut;

        [TestInitialize]
        public void InitializeTest()
        {
            var fhirModel = new FhirModel();
            _sut = new ElementIndexer(fhirModel);
        }

        [TestMethod()]
        public void ElementIndexerTest()
        {
            Assert.IsNotNull(_sut);
            Assert.IsInstanceOfType(_sut, typeof(ElementIndexer));
        }

        [TestMethod()]
        public void FhirDecimalMapTest()
        {
            var input = new FhirDecimal(1081.54M);
            var result = _sut.Map(input);
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOfType(result.First(), typeof(NumberValue));
            Assert.AreEqual(1081.54M, ((NumberValue)result.First()).Value);
        }

        private void CheckPeriod(List<Spark.Search.Expression> result, string start, string end)
        {
            var nrOfComponents = 0;
            if (!string.IsNullOrWhiteSpace(start)) nrOfComponents++;
            if (!string.IsNullOrWhiteSpace(end)) nrOfComponents++;

            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOfType(result.First(), typeof(CompositeValue));
            var comp = result.First() as CompositeValue;
            Assert.AreEqual(nrOfComponents, comp.Components.Count());

            var currentComponent = 0;
            if (!string.IsNullOrWhiteSpace(start))
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

            if (!string.IsNullOrWhiteSpace(end))
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
        public void FhirDateTimeMapTest()
        {
            var input = new FhirDateTime(2015, 3, 14);
            var result = _sut.Map(input);
            CheckPeriod(result, "2015-03-14T00:00:00+00:00", "2015-03-15T00:00:00+00:00");
        }

        [TestMethod()]
        public void PeriodWithStartAndEndMapTest()
        {
            var input = new Period
            {
                StartElement = new FhirDateTime("2015-02"),
                EndElement = new FhirDateTime("2015-03")
            };
            var result = _sut.Map(input);
            CheckPeriod(result, "2015-02-01T00:00:00+00:00", "2015-04-01T00:00:00+00:00");
        }

        [TestMethod()]
        public void PeriodWithJustStartMapTest()
        {
            var input = new Period
            {
                StartElement = new FhirDateTime("2015-02")
            };
            var result = _sut.Map(input);
            CheckPeriod(result, "2015-02-01T00:00:00+00:00", null);
        }

        [TestMethod()]
        public void PeriodWithJustEndMapTest()
        {
            var input = new Period
            {
                EndElement = new FhirDateTime("2015-03")
            };
            var result = _sut.Map(input);
            CheckPeriod(result, null, "2015-04-01T00:00:00+00:00");
        }

        [TestMethod()]
        public void CodingMapTest()
        {
            var input = new Coding
            {
                CodeElement = new Code("bla"),
                SystemElement = new FhirUri("http://bla.com"),
                DisplayElement = new FhirString("bla display")
            };
            var result = _sut.Map(input);

            Assert.AreEqual(1, result.Count());
            Assert.IsInstanceOfType(result[0], typeof(CompositeValue));
            var comp = result[0] as CompositeValue;

            CheckCoding(comp, "bla", "http://bla.com", "bla display");
        }

        private static void CheckCoding(CompositeValue comp, string code, string system, string text)
        {
            CheckCodingFlexible(comp, new Dictionary<string, string>() { { "code", code }, { "system", system }, { "text", text } });
        }

        private static void CheckCodingFlexible(CompositeValue comp, Dictionary<string, string> elements)
        {
            var elementsToCheck = elements.Where(e => e.Value != null);
            var nrOfElements = elementsToCheck.Count();
            Assert.AreEqual(nrOfElements, comp.Components.Count());
            foreach (var c in comp.Components)
            {
                Assert.IsInstanceOfType(c, typeof(IndexValue));
            }

            foreach (var element in elementsToCheck)
            {
                var elementIV = (IndexValue)comp.Components.Where(c => (c as IndexValue).Name == element.Key).FirstOrDefault();
                Assert.IsNotNull(elementIV, $"Expected a component '{element.Key}'");
                Assert.AreEqual(1, elementIV.Values.Count(), $"Expected exactly one component '{element.Key}'");
                Assert.IsInstanceOfType(elementIV.Values[0], typeof(StringValue), $"Expected component '{element.Key}' to be of type {nameof(StringValue)}");
                var codeSV = (StringValue)elementIV.Values[0];
                Assert.AreEqual(element.Value, codeSV.Value, $"Expected component '{element.Key}' to have the value '{element.Value}'");
            }

        }

        [TestMethod()]
        public void CodeableConceptMapTest()
        {
            var input = new CodeableConcept
            {
                Text = "bla text",
                Coding = new List<Coding>()
            };

            var coding1 = new Coding
            {
                CodeElement = new Code("bla"),
                SystemElement = new FhirUri("http://bla.com"),
                DisplayElement = new FhirString("bla display")
            };

            var coding2 = new Coding
            {
                CodeElement = new Code("flit"),
                SystemElement = new FhirUri("http://flit.com"),
                DisplayElement = new FhirString("flit display")
            };

            input.Coding.Add(coding1);
            input.Coding.Add(coding2);

            var result = _sut.Map(input);

            Assert.AreEqual(3, result.Count()); //1 with text and 2 with the codings it

            //Check wether CodeableConcept.Text is in the result.
            var textIVs = result.Where(c => c.GetType() == typeof(IndexValue) && (c as IndexValue).Name == "text").ToList();
            Assert.AreEqual(1, textIVs.Count());
            var textIV = (IndexValue)textIVs.FirstOrDefault();
            Assert.IsNotNull(textIV);
            Assert.AreEqual(1, textIV.Values.Count());
            Assert.IsInstanceOfType(textIV.Values[0], typeof(StringValue));
            Assert.AreEqual("bla text", (textIV.Values[0] as StringValue).Value);

            //Check wether both codings are in the result.
            var codeIVs = result.Where(c => c.GetType() == typeof(CompositeValue)).ToList();
            Assert.AreEqual(2, codeIVs.Count());

            var codeIV1 = (CompositeValue)codeIVs[0];
            var codeIV2 = (CompositeValue)codeIVs[1];
            if (((codeIV1.Components[0] as IndexValue).Values[0] as StringValue).Value == "bla")
            {
                CheckCoding(codeIV1, "bla", "http://bla.com", "bla display");
                CheckCoding(codeIV2, "flit", "http://flit.com", "flit display");
            }
            else //apparently the codings are in different order in the result.
            {
                CheckCoding(codeIV2, "bla", "http://bla.com", "bla display");
                CheckCoding(codeIV1, "flit", "http://flit.com", "flit display");
            }
        }

        [TestMethod()]
        public void IdentifierMapTest()
        {
            var input = new Identifier
            {
                SystemElement = new FhirUri("id-system"),
                ValueElement = new FhirString("id-value")
            };

            var result = _sut.Map(input);

            Assert.AreEqual(1, result.Count());
            Assert.IsInstanceOfType(result[0], typeof(CompositeValue));
            var comp = (CompositeValue)result[0];

            CheckCoding(comp, code: "id-value", system: "id-system", text: null);
        }

        [TestMethod()]
        public void ContactPointMapTest()
        {
            var input = new ContactPoint
            {
                UseElement = new Code<ContactPoint.ContactPointUse>(ContactPoint.ContactPointUse.Mobile),
                ValueElement = new FhirString("cp-value")
            };

            var result = _sut.Map(input);

            Assert.AreEqual(1, result.Count());
            Assert.IsInstanceOfType(result[0], typeof(CompositeValue));
            var comp = (CompositeValue)result[0];

            var codeIV = (IndexValue)comp.Components.Where(c => (c as IndexValue).Name == "code").FirstOrDefault();
            Assert.IsNotNull(codeIV, "Expected a component 'code'");
            Assert.AreEqual(1, codeIV.Values.Count());
            Assert.IsInstanceOfType(codeIV.Values[0], typeof(StringValue));
            var codeSV = (StringValue)codeIV.Values[0];
            Assert.AreEqual("cp-value", codeSV.Value);

            var useIV = (IndexValue)comp.Components.Where(c => (c as IndexValue).Name == "use").FirstOrDefault();
            Assert.IsNotNull(codeIV, "Expected a component 'use'");
            var useCode = (CompositeValue)useIV.Values.Where(c => (c is CompositeValue)).FirstOrDefault();
            Assert.IsNotNull(useCode, $"Expected a value of type {nameof(CompositeValue)} in the 'use' component");
            CheckCoding(useCode, "mobile", null, null);
        }

        [TestMethod()]
        public void FhirBooleanMapTest()
        {
            var input = new FhirBoolean(false);

            var result = _sut.Map(input);

            Assert.AreEqual(1, result.Count());
            Assert.IsInstanceOfType(result[0], typeof(CompositeValue));
            var comp = (CompositeValue)result[0];

            CheckCoding(comp, code: "false", system: null, text: null);
        }

        [TestMethod()]
        public void ResourceReferenceMapTest()
        {
            var input = new ResourceReference
            {
                ReferenceElement = new FhirString("OtherType/OtherId")
            };

            var result = _sut.Map(input);

            Assert.AreEqual(1, result.Count());
            Assert.IsInstanceOfType(result[0], typeof(StringValue));
            var sv = (StringValue)result[0];
            Assert.AreEqual("OtherType/OtherId", sv.Value);
        }

        [TestMethod()]
        public void AddressMapTest()
        {
            var input = new Address
            {
                City = "Amsterdam",
                Country = "Netherlands",
                Line = new List<string> { "Bruggebouw", "Bos en lommerplein 280" },
                PostalCode = "1055 RW"
            };

            var result = _sut.Map(input);

            Assert.AreEqual(5, result.Count()); //2 line elements + city, country and postalcode.
            foreach (var res in result)
            {
                Assert.IsInstanceOfType(res, typeof(StringValue));
            }
            Assert.AreEqual(1, result.Where(r => (r as StringValue).Value == "Bruggebouw").Count());
            Assert.AreEqual(1, result.Where(r => (r as StringValue).Value == "Bos en lommerplein 280").Count());
            Assert.AreEqual(1, result.Where(r => (r as StringValue).Value == "Netherlands").Count());
            Assert.AreEqual(1, result.Where(r => (r as StringValue).Value == "Amsterdam").Count());
            Assert.AreEqual(1, result.Where(r => (r as StringValue).Value == "1055 RW").Count());
        }

        [TestMethod()]
        public void HumanNameMapTest()
        {
            var input = new HumanName();
            input.WithGiven("Pietje").AndFamily("Puk");

            var result = _sut.Map(input);

            Assert.AreEqual(2, result.Count()); //2 line elements + city, country and postalcode.
            foreach (var res in result)
            {
                Assert.IsInstanceOfType(res, typeof(StringValue));
            }
            Assert.AreEqual(1, result.Where(r => (r as StringValue).Value == "Pietje").Count());
            Assert.AreEqual(1, result.Where(r => (r as StringValue).Value == "Puk").Count());
        }

        [TestMethod()]
        public void HumanNameOnlyGivenMapTest()
        {
            var input = new HumanName();
            input.WithGiven("Pietje");

            var result = _sut.Map(input);

            Assert.AreEqual(1, result.Count()); //2 line elements + city, country and postalcode.
            foreach (var res in result)
            {
                Assert.IsInstanceOfType(res, typeof(StringValue));
            }
            Assert.AreEqual(1, result.Where(r => (r as StringValue).Value == "Pietje").Count());
        }

        public void CheckQuantity(List<Spark.Search.Expression> result, decimal? value, string unit, string system, string decimals)
        {
            var nrOfElements = (value.HasValue ? 1 : 0) + new List<String> { unit, system, decimals }.Where(s => s != null).Count();

            Assert.AreEqual(1, result.Count());
            Assert.IsInstanceOfType(result[0], typeof(CompositeValue));
            var comp = (CompositeValue)result[0];

            Assert.AreEqual(nrOfElements, comp.Components.Count());
            Assert.AreEqual(nrOfElements, comp.Components.Where(c => c.GetType() == typeof(IndexValue)).Count());

            if (value.HasValue)
            {
                var compValue = (IndexValue)comp.Components.Where(c => (c as IndexValue).Name == "value").FirstOrDefault();
                Assert.IsNotNull(compValue);
                Assert.AreEqual(1, compValue.Values.Count());
                Assert.IsInstanceOfType(compValue.Values[0], typeof(NumberValue));
                var numberValue = (NumberValue)compValue.Values[0];
                Assert.AreEqual(value.Value, numberValue.Value);
            }

            if (unit != null)
            {
                var compUnit = (IndexValue)comp.Components.Where(c => (c as IndexValue).Name == "unit").FirstOrDefault();
                Assert.IsNotNull(compUnit);
                Assert.AreEqual(1, compUnit.Values.Count());
                Assert.IsInstanceOfType(compUnit.Values[0], typeof(StringValue));
                var stringUnit = (StringValue)compUnit.Values[0];
                Assert.AreEqual(unit, stringUnit.Value);
            }

            if (system != null)
            {
                var compSystem = (IndexValue)comp.Components.Where(c => (c as IndexValue).Name == "system").FirstOrDefault();
                Assert.IsNotNull(compSystem);
                Assert.AreEqual(1, compSystem.Values.Count());
                Assert.IsInstanceOfType(compSystem.Values[0], typeof(StringValue));
                var stringSystem = (StringValue)compSystem.Values[0];
                Assert.AreEqual(system, stringSystem.Value);
            }

            if (decimals != null)
            {
                var compCode = (IndexValue)comp.Components.Where(c => (c as IndexValue).Name == "decimals").FirstOrDefault();
                Assert.IsNotNull(compCode);
                Assert.AreEqual(1, compCode.Values.Count());
                Assert.IsInstanceOfType(compCode.Values[0], typeof(StringValue));
                var stringCode = (StringValue)compCode.Values[0];
                Assert.AreEqual(decimals, stringCode.Value);
            }
        }

        [TestMethod()]
        public void QuantityValueUnitMapTest()
        {
            var input = new Quantity
            {
                Value = 10,
                Unit = "km"
            };

            var result = _sut.Map(input);

            CheckQuantity(result, value: 10, unit: "km", system:null, decimals: null);
        }

        [TestMethod()]
        public void QuantityValueSystemCodeMapTest()
        {
            var input = new Quantity
            {
                Value = 10,
                System = "http://unitsofmeasure.org",
                Code = "kg"
            };

            var result = _sut.Map(input);

            CheckQuantity(result, value: 10000, unit: "g", system: "http://unitsofmeasure.org", decimals: "gE04x1.0");
        }

        [TestMethod()]
        public void CodeMapTest()
        {
            var input = new Code("bla");

            var result = _sut.Map(input);

            Assert.AreEqual(1, result.Count());
            Assert.IsInstanceOfType(result[0], typeof(StringValue));

            Assert.AreEqual("bla", (result[0] as StringValue).Value);
        }

        [TestMethod()]
        public void CodedEnumMapTest()
        {
            var input = new Code<AdministrativeGender>(AdministrativeGender.Male);

            var result = _sut.Map(input);

            Assert.AreEqual(1, result.Count());
            Assert.IsInstanceOfType(result[0], typeof(CompositeValue));

            CheckCoding(result[0] as CompositeValue, "male", null, null);
        }

        [TestMethod()]
        public void FhirStringMapTest()
        {
            var input = new FhirString("bla");

            var result = _sut.Map(input);

            Assert.AreEqual(1, result.Count());
            Assert.IsInstanceOfType(result[0], typeof(StringValue));

            Assert.AreEqual("bla", (result[0] as StringValue).Value);
        }
    }
}
