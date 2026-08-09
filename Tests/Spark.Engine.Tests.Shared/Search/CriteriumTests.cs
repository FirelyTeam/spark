/* 
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2019-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using Xunit;
using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Engine.Search.Support;
using Spark.Engine.Search.Types;

namespace Spark.Search;

public class CriteriumTests
{
    [Fact]
    public void ParseCriterium()
    {
        var searchParameters = new FhirModel().SearchParameters;
        var crit = Criterium.Parse(searchParameters, "Patient", "birthdate", "2018-01-01");
        Assert.Equal("birthdate", crit.ParamName);
        Assert.Null(crit.Modifier);
        Assert.Equal("2018-01-01", crit.Operand.ToString());
        Assert.Equal(Operator.EQ, crit.Operator);

        crit = Criterium.Parse(searchParameters, "Patient", "birthdate", "eq2018-01-01");
        Assert.Equal("birthdate", crit.ParamName);
        Assert.Null(crit.Modifier);
        Assert.Equal("2018-01-01", crit.Operand.ToString());
        Assert.Equal(Operator.EQ, crit.Operator);

        crit = Criterium.Parse(searchParameters, "Patient", "birthdate", "ne2018-01-01");
        Assert.Equal("birthdate", crit.ParamName);
        Assert.Null(crit.Modifier);
        Assert.Equal("2018-01-01", crit.Operand.ToString());
        Assert.Equal(Operator.NOT_EQUAL, crit.Operator);

        crit = Criterium.Parse(searchParameters, "Patient", "birthdate", "gt2018-01-01");
        Assert.Equal("birthdate", crit.ParamName);
        Assert.Null(crit.Modifier);
        Assert.Equal("2018-01-01", crit.Operand.ToString());
        Assert.Equal(Operator.GT, crit.Operator);

        crit = Criterium.Parse(searchParameters, "Patient", "birthdate", "ge2018-01-01");
        Assert.Equal("birthdate", crit.ParamName);
        Assert.Null(crit.Modifier);
        Assert.Equal("2018-01-01", crit.Operand.ToString());
        Assert.Equal(Operator.GTE, crit.Operator);

        crit = Criterium.Parse(searchParameters, "Patient", "birthdate", "lt2018-01-01");
        Assert.Equal("birthdate", crit.ParamName);
        Assert.Null(crit.Modifier);
        Assert.Equal("2018-01-01", crit.Operand.ToString());
        Assert.Equal(Operator.LT, crit.Operator);

        crit = Criterium.Parse(searchParameters, "Patient", "birthdate", "le2018-01-01");
        Assert.Equal("birthdate", crit.ParamName);
        Assert.Null(crit.Modifier);
        Assert.Equal("2018-01-01", crit.Operand.ToString());
        Assert.Equal(Operator.LTE, crit.Operator);

        crit = Criterium.Parse(searchParameters, "Patient", "birthdate:modif1", "ap2018-01-01");
        Assert.Equal("birthdate", crit.ParamName);
        Assert.Equal("2018-01-01", crit.Operand.ToString());
        Assert.Equal("modif1", crit.Modifier);
        Assert.Equal(Operator.APPROX, crit.Operator);

        crit = Criterium.Parse(searchParameters, "Patient", "birthdate:missing", "true");
        Assert.Equal("birthdate", crit.ParamName);
        Assert.Null(crit.Operand);
        Assert.Null(crit.Modifier);
        Assert.Equal(Operator.ISNULL, crit.Operator);

        crit = Criterium.Parse(searchParameters, "Patient", "birthdate:missing", "false");
        Assert.Equal("birthdate", crit.ParamName);
        Assert.Null(crit.Operand);
        Assert.Null(crit.Modifier);
        Assert.Equal(Operator.NOTNULL, crit.Operator);
    }
        
    [Fact]
    public void ParseComparatorOperatorForDate()
    {
        var criterium = Criterium.Parse(new FhirModel().SearchParameters, "Patient", "birthdate", "lt2018-01-01");
        Assert.Equal(Operator.LT, criterium.Operator);
    }

    [Fact]
    public void ParseComparatorOperatorForQuantity()
    {
        var criterium = Criterium.Parse(new FhirModel().SearchParameters, "Observation", "value-quantity", "le5.4|http://unitsofmeasure.org|mg");
        Assert.Equal(Operator.LTE, criterium.Operator);
    }

    [Fact]
    public void ParseComparatorOperatorForNumber()
    {
        var criterium = Criterium.Parse(new FhirModel().SearchParameters, "Encounter", "length", "gt20");
        Assert.Equal(Operator.GT, criterium.Operator);
    }

    [Fact]
    public void ParseChain()
    {
        var crit = Criterium.Parse("par1:type1.par2.par3:text=hoi");
        Assert.Equal(Operator.CHAIN, crit.Operator);
        Assert.Equal("type1", crit.Modifier);
        Assert.True(crit.Operand is Criterium);

        crit = crit.Operand as Criterium;
        Assert.NotNull(crit);
        Assert.Equal(Operator.CHAIN, crit.Operator);
        Assert.Null(crit.Modifier);
        Assert.True(crit.Operand is Criterium);

        crit = crit.Operand as Criterium;
        Assert.NotNull(crit);
        Assert.Equal(Operator.EQ, crit.Operator);
        Assert.Equal("text", crit.Modifier);
        Assert.True(crit.Operand is UntypedValue);
    }

    // FHIR R4 spec on prefixes: https://hl7.org/fhir/R4/search.html#prefix
    [Fact]
    public void ParseChainStripsComparatorPrefixOnInnerParameter()
    {
        // subject:Patient.birthdate=ge1974-12-25 : the comparator prefix belongs to the inner
        // parameter (Patient.birthdate), so the chain must be resolved against Patient - not the
        // outer Observation - for the prefix to be recognised, applied, and stripped from the value.
        var searchParameters = new FhirModel().SearchParameters;
        var crit = Criterium.Parse(searchParameters, "Observation", "subject:Patient.birthdate", "ge1974-12-25");

        Assert.Equal(Operator.CHAIN, crit.Operator);
        Assert.Equal("Patient", crit.Modifier);

        var inner = crit.Operand as Criterium;
        Assert.NotNull(inner);
        Assert.Equal("birthdate", inner.ParamName);
        Assert.Equal(Operator.GTE, inner.Operator);
        Assert.Equal("1974-12-25", inner.Operand.ToString());
    }

    [Theory]
    [InlineData("ge1974-12-25", Operator.GTE, "1974-12-25")]
    [InlineData("le2000-01-01", Operator.LTE, "2000-01-01")]
    [InlineData("gt2000-01-01", Operator.GT, "2000-01-01")]
    [InlineData("lt2000-01-01", Operator.LT, "2000-01-01")]
    [InlineData("eq2000-01-01", Operator.EQ, "2000-01-01")]
    [InlineData("ne2000-01-01", Operator.NOT_EQUAL, "2000-01-01")]
    [InlineData("sa2000-01-01", Operator.STARTS_AFTER, "2000-01-01")]
    [InlineData("eb2000-01-01", Operator.ENDS_BEFORE, "2000-01-01")]
    [InlineData("ap2000-01-01", Operator.APPROX, "2000-01-01")]
    [InlineData("2000-01-01", Operator.EQ, "2000-01-01")] // no prefix: operator stays EQ, value untouched
    public void ParseChainAppliesComparatorPrefixToInnerParameter(string value, Operator expectedOperator, string expectedOperand)
    {
        // The comparator prefix belongs to the inner parameter (Patient.birthdate), so it must be
        // recognised and applied there - regardless of which prefix is used.
        var searchParameters = new FhirModel().SearchParameters;
        var crit = Criterium.Parse(searchParameters, "Observation", "subject:Patient.birthdate", value);

        var inner = crit.Operand as Criterium;
        Assert.NotNull(inner);
        Assert.Equal(expectedOperator, inner.Operator);
        Assert.Equal(expectedOperand, inner.Operand.ToString());
    }

    [Fact]
    public void ParseUntypedChainAppliesComparatorPrefixOnInnerParameter()
    {
        // subject.birthdate=ge1974-12-25 : no explicit :Patient type, so the target must be resolved
        // from the reference's declared targets for the prefix to be recognised and applied.
        var searchParameters = new FhirModel().SearchParameters;
        var crit = Criterium.Parse(searchParameters, "Observation", "subject.birthdate", "ge1974-12-25");

        var inner = crit.Operand as Criterium;
        Assert.NotNull(inner);
        Assert.Equal("birthdate", inner.ParamName);
        Assert.Equal(Operator.GTE, inner.Operator);
        Assert.Equal("1974-12-25", inner.Operand.ToString());
    }

    [Fact]
    public void ParseChainKeepsModifierOnInnerParameter()
    {
        // A modifier on the inner parameter (name:exact) belongs to the inner criterium, not the chain.
        var searchParameters = new FhirModel().SearchParameters;
        var crit = Criterium.Parse(searchParameters, "Observation", "subject:Patient.name:exact", "Smith");

        var inner = crit.Operand as Criterium;
        Assert.NotNull(inner);
        Assert.Equal("name", inner.ParamName);
        Assert.Equal("exact", inner.Modifier);
        Assert.Equal("Smith", inner.Operand.ToString());
    }

    [Fact]
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

        Assert.Equal("par1:type1.par2.par3:text=hoi", crit.ToString());
    }


    [Fact]
    public void SerializeCriterium()
    {
        var crit = new Criterium { ParamName = "paramX", Modifier = "modif1", Operand = new NumberValue(18), Operator = Operator.GTE };
        Assert.Equal("paramX:modif1=ge18", crit.ToString());

        crit = new Criterium { ParamName = "paramX", Operand = new NumberValue(18) };
        Assert.Equal("paramX=18", crit.ToString());

        crit = new Criterium { ParamName = "paramX", Operator = Operator.ISNULL };
        Assert.Equal("paramX:missing=true", crit.ToString());

        crit = new Criterium { ParamName = "paramX", Operator = Operator.NOTNULL };
        Assert.Equal("paramX:missing=false", crit.ToString());
    }


    [Fact]
    public void HandleNumberParam()
    {
        var p1 = new NumberValue(18);
        Assert.Equal("18", p1.ToString());

        var p2 = NumberValue.Parse("18");
        Assert.Equal(18M, p2.Value);

        var p3 = NumberValue.Parse("18.00");
        Assert.Equal(18.00M, p3.Value);

        var crit = Criterium.Parse("paramX=18.34");
        var p4 = ((UntypedValue)crit.Operand).AsNumberValue();
        Assert.Equal(18.34M, p4.Value);
    }

    [Fact]
    public void HandleDateParam()
    {
        // Brian: Not sure tha these tests SHOULD pass...
        // a time component on the Date?
        var p1 = new DateValue(new DateTimeOffset(1972, 11, 30, 15, 20, 49, TimeSpan.Zero));
        Assert.Equal("1972-11-30", p1.ToString());

        // we can parse a valid FHIR datetime and strip the time part off
        // (but it must be a valid FHIR datetime)
        var p2 = DateValue.Parse("1972-11-30T18:45:36Z");
        Assert.Equal("1972-11-30", p2.ToString());

        var crit = Criterium.Parse("paramX=1972-11-30");
        var p3 = ((UntypedValue)crit.Operand).AsDateValue();
        Assert.Equal("1972-11-30", p3.Value);

        try
        {
            // Test with an invalid FHIR datetime (no timezone specified)
            _ = DateValue.Parse("1972-11-30T18:45:36");
            Assert.Fail("The datetime [1972-11-30T18:45:36] does not have a timezone, hence should fail parsing as a datevalue (via fhirdatetime)");
        }
        catch (ArgumentException)
        {
        }
    }

    [Fact]
    public void HandleDateTimeParam()
    {
        var p1 = new FhirDateTime(new DateTimeOffset(1972, 11, 30, 15, 20, 49, TimeSpan.Zero));
        Assert.Equal("1972-11-30T15:20:49+00:00", p1.Value);

        var crit = Criterium.Parse("paramX=1972-11-30T18:45:36Z");
        var p3 = ((UntypedValue)crit.Operand).AsDateValue();
        Assert.Equal("1972-11-30", p3.Value);

        var p4 = ((UntypedValue)crit.Operand).AsDateTimeValue();
        Assert.Equal("1972-11-30T18:45:36Z", p4.Value);
    }

    [Fact]
    public void HandleStringParam()
    {
        var p1 = new StringValue("Hello, world");
        Assert.Equal(@"Hello\, world", p1.ToString());

        var p2 = new StringValue("Pay $300|Pay $100|");
        Assert.Equal(@"Pay \$300\|Pay \$100\|", p2.ToString());

        var p3 = StringValue.Parse(@"Pay \$300\|Pay \$100\|");
        Assert.Equal("Pay $300|Pay $100|", p3.Value);

        var crit = Criterium.Parse(@"paramX=Hello\, world");
        var p4 = ((UntypedValue)crit.Operand).AsStringValue();
        Assert.Equal("Hello, world", p4.Value);
    }


    [Fact]
    public void HandleTokenParam()
    {
        var p1 = new TokenValue { Namespace = "http://somewhere.nl/codes", Value = "NOK" };
        Assert.Equal("http://somewhere.nl/codes|NOK", p1.ToString());

        var p2 = new TokenValue { Namespace = "http://some|where.nl/codes", Value = "y|n" };
        Assert.Equal(@"http://some\|where.nl/codes|y\|n", p2.ToString());

        var p3 = new TokenValue { Value = "NOK", AnyNamespace = true };
        Assert.Equal("NOK", p3.ToString());

        var p4 = new TokenValue { Value = "NOK", AnyNamespace = false };
        Assert.Equal("|NOK", p4.ToString());

        var p5 = TokenValue.Parse("http://somewhere.nl/codes|NOK");
        Assert.Equal("http://somewhere.nl/codes", p5.Namespace);
        Assert.Equal("NOK", p5.Value);
        Assert.False(p4.AnyNamespace);

        var p6 = TokenValue.Parse(@"http://some\|where.nl/codes|y\|n");
        Assert.Equal(@"http://some|where.nl/codes", p6.Namespace);
        Assert.Equal("y|n", p6.Value);
        Assert.False(p6.AnyNamespace);

        var p7 = TokenValue.Parse("|NOK");
        Assert.Null(p7.Namespace);
        Assert.Equal("NOK", p7.Value);
        Assert.False(p7.AnyNamespace);

        var p8 = TokenValue.Parse("NOK");
        Assert.Null(p8.Namespace);
        Assert.Equal("NOK", p8.Value);
        Assert.True(p8.AnyNamespace);

        var crit = Criterium.Parse("paramX=|NOK");
        var p9 = ((UntypedValue)crit.Operand).AsTokenValue();
        Assert.Equal("NOK", p9.Value);
        Assert.False(p9.AnyNamespace);
    }


    [Fact]
    public void HandleQuantityParam()
    {
        var p1 = new QuantityValue(3.141M, "http://unitsofmeasure.org", "mg");
        Assert.Equal("3.141|http://unitsofmeasure.org|mg", p1.ToString());

        var p2 = new QuantityValue(3.141M, "mg");
        Assert.Equal("3.141||mg", p2.ToString());

        var p3 = new QuantityValue(3.141M, "http://system.com/id$4", "$/d");
        Assert.Equal(@"3.141|http://system.com/id\$4|\$/d", p3.ToString());

        var p4 = QuantityValue.Parse("3.141|http://unitsofmeasure.org|mg");
        Assert.Equal(3.141M, p4.Number);
        Assert.Equal("http://unitsofmeasure.org", p4.Namespace);
        Assert.Equal("mg", p4.Unit);

        var p5 = QuantityValue.Parse("3.141||mg");
        Assert.Equal(3.141M, p5.Number);
        Assert.Null(p5.Namespace);
        Assert.Equal("mg", p5.Unit);

        var p6 = QuantityValue.Parse(@"3.141|http://system.com/id\$4|\$/d");
        Assert.Equal(3.141M, p6.Number);
        Assert.Equal("http://system.com/id$4", p6.Namespace);
        Assert.Equal("$/d", p6.Unit);

        var crit = Criterium.Parse("paramX=3.14||mg");
        var p7 = ((UntypedValue)crit.Operand).AsQuantityValue();
        Assert.Equal(3.14M, p7.Number);
        Assert.Null(p7.Namespace);
        Assert.Equal("mg", p7.Unit);
    }


    [Fact]
    public void SplitNotEscaped()
    {
        var res = "hallo".SplitNotEscaped('$');
        Assert.Equivalent(res, new[] { "hallo" });

        res = "part1$part2".SplitNotEscaped('$');
        Assert.Equivalent(res, new[] { "part1", "part2" });

        res = "part1$".SplitNotEscaped('$');
        Assert.Equivalent(res, new[] { "part1", string.Empty });

        res = "$part2".SplitNotEscaped('$');
        Assert.Equivalent(res, new[] { string.Empty, "part2" });

        res = "$".SplitNotEscaped('$');
        Assert.Equivalent(res, new[] { string.Empty, string.Empty });

        res = "a$$c".SplitNotEscaped('$');
        Assert.Equivalent(res, new[] { "a", string.Empty, "c" });

        res = @"p\@rt1$p\@rt2".SplitNotEscaped('$');
        Assert.Equivalent(res, new[] { @"p\@rt1", @"p\@rt2" });

        res = @"mes\$age1$mes\$age2".SplitNotEscaped('$');
        Assert.Equivalent(res, new[] { @"mes\$age1", @"mes\$age2" });

        res = string.Empty.SplitNotEscaped('$');
        Assert.Equivalent(res, new[] { string.Empty });
    }


    [Fact]
    public void HandleReferenceParam()
    {
        var p1 = new ReferenceValue("2");
        Assert.Equal("2", p1.Value);

        var p2 = new ReferenceValue("http://server.org/fhir/Patient/1");
        Assert.Equal("http://server.org/fhir/Patient/1", p2.Value);

        var crit = Criterium.Parse(@"paramX=http://server.org/\$4/fhir/Patient/1");
        var p3 = ((UntypedValue)crit.Operand).AsReferenceValue();
        Assert.Equal("http://server.org/$4/fhir/Patient/1", p3.Value);
    }

    [Fact]
    public void HandleMultiValueParam()
    {
        var p1 = new ChoiceValue(new ValueExpression[] { new StringValue("hello, world!"), new NumberValue(18.4M) });
        Assert.Equal(@"hello\, world!,18.4", p1.ToString());

        var p2 = ChoiceValue.Parse(@"hello\, world!,18.4");
        Assert.Equal(2, p2.Choices.Length);
        Assert.Equal("hello, world!", ((UntypedValue)p2.Choices[0]).AsStringValue().Value);
        Assert.Equal(18.4M, ((UntypedValue)p2.Choices[1]).AsNumberValue().Value);
    }

    [Fact]
    public void HandleComposites()
    {
        var pX = new CompositeValue(new ValueExpression[] { new StringValue("hello, world!"), new NumberValue(14.8M) });
        var pY = new TokenValue { Namespace = "http://somesuch.org", Value = "NOK" };
        var p1 = new ChoiceValue(new ValueExpression[] { pX, pY });
        Assert.Equal(@"hello\, world!$14.8,http://somesuch.org|NOK", p1.ToString());

        var crit1 = ChoiceValue.Parse(@"hello\, world$14.8,http://somesuch.org|NOK");
        Assert.Equal(2, crit1.Choices.Length);
        Assert.True(crit1.Choices[0] is CompositeValue);
        var comp1 = crit1.Choices[0] as CompositeValue;
        Assert.NotNull(comp1);
        Assert.Equal(2, comp1.Components.Length);
        Assert.Equal("hello, world", ((UntypedValue)comp1.Components[0]).AsStringValue().Value);
        Assert.Equal(14.8M, ((UntypedValue)comp1.Components[1]).AsNumberValue().Value);
        Assert.Equal("http://somesuch.org|NOK", ((UntypedValue)crit1.Choices[1]).AsTokenValue().ToString());
    }
}
