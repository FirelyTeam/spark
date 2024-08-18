/* 
 * Copyright (c) 2015-2018, Firely (info@fire.ly) and contributors
 * Copyright (c) 2019-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hl7.Fhir.Model;
using Spark.Search.Support;


namespace Spark.Search
{
    [TestClass]
#if PORTABLE45
    public class PortableCriteriumTests
#else
    public class CriteriumTests
#endif
    {
        [TestMethod]
        public void ParseCriterium()
        {
            var crit = Criterium.Parse("Patient", "birthdate", "2018-01-01");
            Assert.AreEqual("birthdate", crit.ParamName);
            Assert.IsNull(crit.Modifier);
            Assert.AreEqual("2018-01-01", crit.Operand.ToString());
            Assert.AreEqual(Operator.EQ, crit.Operator);

            crit = Criterium.Parse("Patient", "birthdate", "eq2018-01-01");
            Assert.AreEqual("birthdate", crit.ParamName);
            Assert.IsNull(crit.Modifier);
            Assert.AreEqual("2018-01-01", crit.Operand.ToString());
            Assert.AreEqual(Operator.EQ, crit.Operator);

            crit = Criterium.Parse("Patient", "birthdate", "ne2018-01-01");
            Assert.AreEqual("birthdate", crit.ParamName);
            Assert.IsNull(crit.Modifier);
            Assert.AreEqual("2018-01-01", crit.Operand.ToString());
            Assert.AreEqual(Operator.NOT_EQUAL, crit.Operator);

            crit = Criterium.Parse("Patient", "birthdate", "gt2018-01-01");
            Assert.AreEqual("birthdate", crit.ParamName);
            Assert.IsNull(crit.Modifier);
            Assert.AreEqual("2018-01-01", crit.Operand.ToString());
            Assert.AreEqual(Operator.GT, crit.Operator);

            crit = Criterium.Parse("Patient", "birthdate", "ge2018-01-01");
            Assert.AreEqual("birthdate", crit.ParamName);
            Assert.IsNull(crit.Modifier);
            Assert.AreEqual("2018-01-01", crit.Operand.ToString());
            Assert.AreEqual(Operator.GTE, crit.Operator);

            crit = Criterium.Parse("Patient", "birthdate", "lt2018-01-01");
            Assert.AreEqual("birthdate", crit.ParamName);
            Assert.IsNull(crit.Modifier);
            Assert.AreEqual("2018-01-01", crit.Operand.ToString());
            Assert.AreEqual(Operator.LT, crit.Operator);

            crit = Criterium.Parse("Patient", "birthdate", "le2018-01-01");
            Assert.AreEqual("birthdate", crit.ParamName);
            Assert.IsNull(crit.Modifier);
            Assert.AreEqual("2018-01-01", crit.Operand.ToString());
            Assert.AreEqual(Operator.LTE, crit.Operator);

            crit = Criterium.Parse("Patient", "birthdate:modif1", "ap2018-01-01");
            Assert.AreEqual("birthdate", crit.ParamName);
            Assert.AreEqual("2018-01-01", crit.Operand.ToString());
            Assert.AreEqual("modif1", crit.Modifier);
            Assert.AreEqual(Operator.APPROX, crit.Operator);

            crit = Criterium.Parse("Patient", "birthdate:missing", "true");
            Assert.AreEqual("birthdate", crit.ParamName);
            Assert.IsNull(crit.Operand);
            Assert.IsNull(crit.Modifier);
            Assert.AreEqual(Operator.ISNULL, crit.Operator);

            crit = Criterium.Parse("Patient", "birthdate:missing", "false");
            Assert.AreEqual("birthdate", crit.ParamName);
            Assert.IsNull(crit.Operand);
            Assert.IsNull(crit.Modifier);
            Assert.AreEqual(Operator.NOTNULL, crit.Operator);
        }
        
        [TestMethod]
        public void ParseComparatorOperatorForDate()
        {
            var criterium = Criterium.Parse("Patient", "birthdate", "lt2018-01-01");
            Assert.AreEqual(Operator.LT, criterium.Operator);
        }

        [TestMethod]
        public void ParseComparatorOperatorForQuantity()
        {
            var criterium = Criterium.Parse("Observation", "value-quantity", "le5.4|http://unitsofmeasure.org|mg");
            Assert.AreEqual(Operator.LTE, criterium.Operator);
        }

        [TestMethod]
        public void ParseComparatorOperatorForNumber()
        {
            var criterium = Criterium.Parse("Encounter", "length", "gt20");
            Assert.AreEqual(Operator.GT, criterium.Operator);
        }

        [TestMethod]
        public void ParseChain()
        {
#pragma warning disable 618
            var crit = Criterium.Parse("par1:type1.par2.par3:text=hoi");
#pragma warning restore 618
            Assert.IsTrue(crit.Operator == Operator.CHAIN);
            Assert.AreEqual("type1", crit.Modifier);
            Assert.IsTrue(crit.Operand is Criterium);

            crit = crit.Operand as Criterium;
            Assert.IsTrue(crit.Operator == Operator.CHAIN);
            Assert.AreEqual(null, crit.Modifier);
            Assert.IsTrue(crit.Operand is Criterium);

            crit = crit.Operand as Criterium;
            Assert.IsTrue(crit.Operator == Operator.EQ);
            Assert.AreEqual("text", crit.Modifier);
            Assert.IsTrue(crit.Operand is UntypedValue);
        }

        [TestMethod]
        public void SerializeChain()
        {
            var crit = new Criterium
            {
                ParamName = "par1",
                Modifier = "type1",
                Operator = Operator.CHAIN,
                Operand =
                    new Criterium
                    {
                        ParamName = "par2",
                        Operator = Operator.CHAIN,
                        Operand =
                            new Criterium { ParamName = "par3", Modifier = "text", Operator = Operator.EQ, Operand = new StringValue("hoi") }
                    }
            };

            Assert.AreEqual("par1:type1.par2.par3:text=hoi", crit.ToString());
        }


        [TestMethod]
        public void SerializeCriterium()
        {
            var crit = new Criterium { ParamName = "paramX", Modifier = "modif1", Operand = new NumberValue(18), Operator = Operator.GTE };
            Assert.AreEqual("paramX:modif1=ge18", crit.ToString());

            crit = new Criterium { ParamName = "paramX", Operand = new NumberValue(18) };
            Assert.AreEqual("paramX=18", crit.ToString());

            crit = new Criterium { ParamName = "paramX", Operator = Operator.ISNULL };
            Assert.AreEqual("paramX:missing=true", crit.ToString());

            crit = new Criterium { ParamName = "paramX", Operator = Operator.NOTNULL };
            Assert.AreEqual("paramX:missing=false", crit.ToString());
        }


        [TestMethod]
        public void HandleNumberParam()
        {
            var p1 = new NumberValue(18);
            Assert.AreEqual("18", p1.ToString());

            var p2 = NumberValue.Parse("18");
            Assert.AreEqual(18M, p2.Value);

            var p3 = NumberValue.Parse("18.00");
            Assert.AreEqual(18.00M, p3.Value);

#pragma warning disable 618
            var crit = Criterium.Parse("paramX=18.34");
#pragma warning restore 618
            var p4 = ((UntypedValue)crit.Operand).AsNumberValue();
            Assert.AreEqual(18.34M, p4.Value);
        }

        [TestMethod]
        public void HandleDateParam()
        {
            // Brian: Not sure tha these tests SHOULD pass...
            // a time component on the Date?
            var p1 = new DateValue(new DateTimeOffset(1972, 11, 30, 15, 20, 49, TimeSpan.Zero));
            Assert.AreEqual("1972-11-30", p1.ToString());

            // we can parse a valid FHIR datetime and strip the time part off
            // (but it must be a valid FHIR datetime)
            var p2 = DateValue.Parse("1972-11-30T18:45:36Z");
            Assert.AreEqual("1972-11-30", p2.ToString());

#pragma warning disable 618
            var crit = Criterium.Parse("paramX=1972-11-30");
#pragma warning restore 618
            var p3 = ((UntypedValue)crit.Operand).AsDateValue();
            Assert.AreEqual("1972-11-30", p3.Value);

            try
            {
                // Test with an invalid FHIR datetime (no timezone specified)
                var p4 = DateValue.Parse("1972-11-30T18:45:36");
                Assert.Fail("The datetime [1972-11-30T18:45:36] does not have a timezone, hence should fail parsing as a datevalue (via fhirdatetime)");
            }
            catch (ArgumentException)
            {
            }
        }

        [TestMethod]
        public void HandleDateTimeParam()
        {
            var p1 = new FhirDateTime(new DateTimeOffset(1972, 11, 30, 15, 20, 49, TimeSpan.Zero));
            Assert.AreEqual("1972-11-30T15:20:49+00:00", p1.Value.ToString());

#pragma warning disable 618
            var crit = Criterium.Parse("paramX=1972-11-30T18:45:36Z");
#pragma warning restore 618
            var p3 = ((UntypedValue)crit.Operand).AsDateValue();
            Assert.AreEqual("1972-11-30", p3.Value);

            var p4 = ((UntypedValue)crit.Operand).AsDateTimeValue();
            Assert.AreEqual("1972-11-30T18:45:36Z", p4.Value);
        }

        [TestMethod]
        public void HandleStringParam()
        {
            var p1 = new StringValue("Hello, world");
            Assert.AreEqual(@"Hello\, world", p1.ToString());

            var p2 = new StringValue("Pay $300|Pay $100|");
            Assert.AreEqual(@"Pay \$300\|Pay \$100\|", p2.ToString());

            var p3 = StringValue.Parse(@"Pay \$300\|Pay \$100\|");
            Assert.AreEqual("Pay $300|Pay $100|", p3.Value);

#pragma warning disable 618
            var crit = Criterium.Parse(@"paramX=Hello\, world");
#pragma warning restore 618
            var p4 = ((UntypedValue)crit.Operand).AsStringValue();
            Assert.AreEqual("Hello, world", p4.Value);
        }


        [TestMethod]
        public void HandleTokenParam()
        {
            var p1 = new TokenValue { Namespace = "http://somewhere.nl/codes", Value = "NOK" };
            Assert.AreEqual("http://somewhere.nl/codes|NOK", p1.ToString());

            var p2 = new TokenValue { Namespace = "http://some|where.nl/codes", Value = "y|n" };
            Assert.AreEqual(@"http://some\|where.nl/codes|y\|n", p2.ToString());

            var p3 = new TokenValue { Value = "NOK", AnyNamespace = true };
            Assert.AreEqual("NOK", p3.ToString());

            var p4 = new TokenValue { Value = "NOK", AnyNamespace = false };
            Assert.AreEqual("|NOK", p4.ToString());

            var p5 = TokenValue.Parse("http://somewhere.nl/codes|NOK");
            Assert.AreEqual("http://somewhere.nl/codes", p5.Namespace);
            Assert.AreEqual("NOK", p5.Value);
            Assert.IsFalse(p4.AnyNamespace);

            var p6 = TokenValue.Parse(@"http://some\|where.nl/codes|y\|n");
            Assert.AreEqual(@"http://some|where.nl/codes", p6.Namespace);
            Assert.AreEqual("y|n", p6.Value);
            Assert.IsFalse(p6.AnyNamespace);

            var p7 = TokenValue.Parse("|NOK");
            Assert.AreEqual(null, p7.Namespace);
            Assert.AreEqual("NOK", p7.Value);
            Assert.IsFalse(p7.AnyNamespace);

            var p8 = TokenValue.Parse("NOK");
            Assert.AreEqual(null, p8.Namespace);
            Assert.AreEqual("NOK", p8.Value);
            Assert.IsTrue(p8.AnyNamespace);

#pragma warning disable 618
            var crit = Criterium.Parse("paramX=|NOK");
#pragma warning restore 618
            var p9 = ((UntypedValue)crit.Operand).AsTokenValue();
            Assert.AreEqual("NOK", p9.Value);
            Assert.IsFalse(p9.AnyNamespace);
        }


        [TestMethod]
        public void HandleQuantityParam()
        {
            var p1 = new QuantityValue(3.141M, "http://unitsofmeasure.org", "mg");
            Assert.AreEqual("3.141|http://unitsofmeasure.org|mg", p1.ToString());

            var p2 = new QuantityValue(3.141M, "mg");
            Assert.AreEqual("3.141||mg", p2.ToString());

            var p3 = new QuantityValue(3.141M, "http://system.com/id$4", "$/d");
            Assert.AreEqual(@"3.141|http://system.com/id\$4|\$/d", p3.ToString());

            var p4 = QuantityValue.Parse("3.141|http://unitsofmeasure.org|mg");
            Assert.AreEqual(3.141M, p4.Number);
            Assert.AreEqual("http://unitsofmeasure.org", p4.Namespace);
            Assert.AreEqual("mg", p4.Unit);

            var p5 = QuantityValue.Parse("3.141||mg");
            Assert.AreEqual(3.141M, p5.Number);
            Assert.IsNull(p5.Namespace);
            Assert.AreEqual("mg", p5.Unit);

            var p6 = QuantityValue.Parse(@"3.141|http://system.com/id\$4|\$/d");
            Assert.AreEqual(3.141M, p6.Number);
            Assert.AreEqual("http://system.com/id$4", p6.Namespace);
            Assert.AreEqual("$/d", p6.Unit);

#pragma warning disable 618
            var crit = Criterium.Parse("paramX=3.14||mg");
#pragma warning restore 618
            var p7 = ((UntypedValue)crit.Operand).AsQuantityValue();
            Assert.AreEqual(3.14M, p7.Number);
            Assert.IsNull(p7.Namespace);
            Assert.AreEqual("mg", p7.Unit);
        }


        [TestMethod]
        public void SplitNotEscaped()
        {
            var res = "hallo".SplitNotEscaped('$');
            CollectionAssert.AreEquivalent(res, new string[] { "hallo" });

            res = "part1$part2".SplitNotEscaped('$');
            CollectionAssert.AreEquivalent(res, new string[] { "part1", "part2" });

            res = "part1$".SplitNotEscaped('$');
            CollectionAssert.AreEquivalent(res, new string[] { "part1", string.Empty });

            res = "$part2".SplitNotEscaped('$');
            CollectionAssert.AreEquivalent(res, new string[] { string.Empty, "part2" });

            res = "$".SplitNotEscaped('$');
            CollectionAssert.AreEquivalent(res, new string[] { string.Empty, string.Empty });

            res = "a$$c".SplitNotEscaped('$');
            CollectionAssert.AreEquivalent(res, new string[] { "a", string.Empty, "c" });

            res = @"p\@rt1$p\@rt2".SplitNotEscaped('$');
            CollectionAssert.AreEquivalent(res, new string[] { @"p\@rt1", @"p\@rt2" });

            res = @"mes\$age1$mes\$age2".SplitNotEscaped('$');
            CollectionAssert.AreEquivalent(res, new string[] { @"mes\$age1", @"mes\$age2" });

            res = string.Empty.SplitNotEscaped('$');
            CollectionAssert.AreEquivalent(res, new string[] { string.Empty });
        }


        [TestMethod]
        public void HandleReferenceParam()
        {
            var p1 = new ReferenceValue("2");
            Assert.AreEqual("2", p1.Value);

            var p2 = new ReferenceValue("http://server.org/fhir/Patient/1");
            Assert.AreEqual("http://server.org/fhir/Patient/1", p2.Value);

#pragma warning disable 618
            var crit = Criterium.Parse(@"paramX=http://server.org/\$4/fhir/Patient/1");
#pragma warning restore 618
            var p3 = ((UntypedValue)crit.Operand).AsReferenceValue();
            Assert.AreEqual("http://server.org/$4/fhir/Patient/1", p3.Value);
        }

        [TestMethod]
        public void HandleMultiValueParam()
        {
            var p1 = new ChoiceValue(new ValueExpression[] { new StringValue("hello, world!"), new NumberValue(18.4M) });
            Assert.AreEqual(@"hello\, world!,18.4", p1.ToString());

            var p2 = ChoiceValue.Parse(@"hello\, world!,18.4");
            Assert.AreEqual(2, p2.Choices.Length);
            Assert.AreEqual("hello, world!", ((UntypedValue)p2.Choices[0]).AsStringValue().Value);
            Assert.AreEqual(18.4M, ((UntypedValue)p2.Choices[1]).AsNumberValue().Value);
        }

        [TestMethod]
        public void HandleComposites()
        {
            var pX = new CompositeValue(new ValueExpression[] { new StringValue("hello, world!"), new NumberValue(14.8M) });
            var pY = new TokenValue { Namespace = "http://somesuch.org", Value = "NOK" };
            var p1 = new ChoiceValue(new ValueExpression[] { pX, pY });
            Assert.AreEqual(@"hello\, world!$14.8,http://somesuch.org|NOK", p1.ToString());

            var crit1 = ChoiceValue.Parse(@"hello\, world$14.8,http://somesuch.org|NOK");
            Assert.AreEqual(2, crit1.Choices.Length);
            Assert.IsTrue(crit1.Choices[0] is CompositeValue);
            var comp1 = crit1.Choices[0] as CompositeValue;
            Assert.AreEqual(2, comp1.Components.Length);
            Assert.AreEqual("hello, world", ((UntypedValue)comp1.Components[0]).AsStringValue().Value);
            Assert.AreEqual(14.8M, ((UntypedValue)comp1.Components[1]).AsNumberValue().Value);
            Assert.AreEqual("http://somesuch.org|NOK", ((UntypedValue)crit1.Choices[1]).AsTokenValue().ToString());
        }
    }
}
