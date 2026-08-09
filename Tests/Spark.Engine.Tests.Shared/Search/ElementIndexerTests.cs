/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Xunit;
using Spark.Engine.Core;
using Spark.Engine.Model;
using Spark.Engine.Search.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using Expression = Spark.Engine.Search.Types.Expression;

namespace Spark.Engine.Search.Tests;

public class ElementIndexerTests
{
    private ElementIndexer _sut;

    public ElementIndexerTests()
    {
        var fhirModel = new FhirModel();
        _sut = new ElementIndexer(fhirModel);
    }

    [Fact]
    public void ElementIndexerTest()
    {
        Assert.NotNull(_sut);
        Assert.IsType<ElementIndexer>(_sut);
    }

    [Fact]
    public void FhirDecimalMapTest()
    {
        var input = new FhirDecimal(1081.54M);
        var result = _sut.Map(input);
        Assert.Single(result);
        Assert.IsType<NumberValue>(result.First());
        Assert.Equal(1081.54M, ((NumberValue)result.First()).Value);
    }

    private void CheckPeriod(List<Expression> result, string start, string end)
    {
        var nrOfComponents = 0;
        if (!string.IsNullOrWhiteSpace(start)) nrOfComponents++;
        if (!string.IsNullOrWhiteSpace(end)) nrOfComponents++;

        Assert.Single(result);
        Assert.IsType<CompositeValue>(result.First());
        var comp = result.FirstOrDefault() as CompositeValue;
        Assert.NotNull(comp);
        Assert.Equal(nrOfComponents, comp.Components.Count());

        var currentComponent = 0;
        if (!string.IsNullOrWhiteSpace(start))
        {
            Assert.IsType<IndexValue>(comp.Components[currentComponent]);
            var ixValue = comp.Components[currentComponent] as IndexValue;
            Assert.Equal("start", ixValue.Name);
            Assert.Single(ixValue.Values);
            Assert.IsType<DateTimeValue>(ixValue.Values[0]);
            var dtValue = ixValue.Values[0] as DateTimeValue;
            Assert.Equal(new DateTimeValue(start).Value, dtValue.Value);
            currentComponent++;
        }

        if (!string.IsNullOrWhiteSpace(end))
        {
            Assert.IsType<IndexValue>(comp.Components[currentComponent]);
            var ixValue = comp.Components[currentComponent] as IndexValue;
            Assert.Equal("end", ixValue.Name);
            Assert.Single(ixValue.Values);
            Assert.IsType<DateTimeValue>(ixValue.Values[0]);
            var dtValue = ixValue.Values[0] as DateTimeValue;
            Assert.Equal(new DateTimeValue(end).Value, dtValue.Value);
        }
    }
    [Fact]
    public void FhirDateTimeMapTest()
    {
        var input = new FhirDateTime(2015, 3, 14);
        var result = _sut.Map(input);
        CheckPeriod(result, "2015-03-14T00:00:00+00:00", "2015-03-15T00:00:00+00:00");
    }

    [Fact]
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

    [Fact]
    public void PeriodWithJustStartMapTest()
    {
        var input = new Period
        {
            StartElement = new FhirDateTime("2015-02")
        };
        var result = _sut.Map(input);
        CheckPeriod(result, "2015-02-01T00:00:00+00:00", null);
    }

    [Fact]
    public void PeriodWithJustEndMapTest()
    {
        var input = new Period
        {
            EndElement = new FhirDateTime("2015-03")
        };
        var result = _sut.Map(input);
        CheckPeriod(result, null, "2015-04-01T00:00:00+00:00");
    }

    [Fact]
    public void CodingMapTest()
    {
        var input = new Coding
        {
            CodeElement = new Code("bla"),
            SystemElement = new FhirUri("http://bla.com"),
            DisplayElement = new FhirString("bla display")
        };
        var result = _sut.Map(input);

        Assert.Single(result);
        Assert.IsType<CompositeValue>(result[0]);
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
        Assert.Equal(nrOfElements, comp.Components.Count());
        foreach (var c in comp.Components)
        {
            Assert.IsType<IndexValue>(c);
        }

        foreach (var element in elementsToCheck)
        {
            var elementIV = (IndexValue)comp.Components.Where(c => (c as IndexValue).Name == element.Key).FirstOrDefault();
            Assert.NotNull(elementIV);
            Assert.Single(elementIV.Values);
            Assert.IsType<StringValue>(elementIV.Values[0]);
            var codeSV = (StringValue)elementIV.Values[0];
            Assert.Equal(element.Value, codeSV.Value);
        }

    }

    [Fact]
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

        Assert.Equal(3, result.Count()); //1 with text and 2 with the codings it

        //Check wether CodeableConcept.Text is in the result.
        var textIVs = result.Where(c => c.GetType() == typeof(IndexValue) && (c as IndexValue).Name == "text").ToList();
        Assert.Single(textIVs);
        var textIV = (IndexValue)textIVs.FirstOrDefault();
        Assert.NotNull(textIV);
        Assert.Single(textIV.Values);
        Assert.IsType<StringValue>(textIV.Values[0]);
        Assert.Equal("bla text", (textIV.Values[0] as StringValue).Value);

        //Check wether both codings are in the result.
        var codeIVs = result.Where(c => c.GetType() == typeof(CompositeValue)).ToList();
        Assert.Equal(2, codeIVs.Count());

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

    [Fact]
    public void IdentifierMapTest()
    {
        var input = new Identifier
        {
            SystemElement = new FhirUri("id-system"),
            ValueElement = new FhirString("id-value")
        };

        var result = _sut.Map(input);

        Assert.Single(result);
        Assert.IsType<CompositeValue>(result[0]);
        var comp = (CompositeValue)result[0];

        CheckCoding(comp, code: "id-value", system: "id-system", text: null);
    }

    [Fact]
    public void ContactPointMapTest()
    {
        var input = new ContactPoint
        {
            UseElement = new Code<ContactPoint.ContactPointUse>(ContactPoint.ContactPointUse.Mobile),
            ValueElement = new FhirString("cp-value")
        };

        var result = _sut.Map(input);

        Assert.Single(result);
        Assert.IsType<CompositeValue>(result[0]);
        var comp = (CompositeValue)result[0];

        var codeIV = (IndexValue)comp.Components.Where(c => (c as IndexValue).Name == "code").FirstOrDefault();
        Assert.NotNull(codeIV);
        Assert.Single(codeIV.Values);
        Assert.IsType<StringValue>(codeIV.Values[0]);
        var codeSV = (StringValue)codeIV.Values[0];
        Assert.Equal("cp-value", codeSV.Value);

        var useIV = (IndexValue)comp.Components.Where(c => (c as IndexValue).Name == "use").FirstOrDefault();
        Assert.NotNull(codeIV);
        var useCode = (CompositeValue)useIV.Values.Where(c => (c is CompositeValue)).FirstOrDefault();
        Assert.NotNull(useCode);
        CheckCoding(useCode, "mobile", null, null);
    }

    [Fact]
    public void FhirBooleanMapTest()
    {
        var input = new FhirBoolean(false);

        var result = _sut.Map(input);

        Assert.Single(result);
        Assert.IsType<CompositeValue>(result[0]);
        var comp = (CompositeValue)result[0];

        CheckCoding(comp, code: "false", system: null, text: null);
    }

    [Fact]
    public void ResourceReferenceMapTest()
    {
        var input = new ResourceReference
        {
            ReferenceElement = new FhirString("OtherType/OtherId")
        };

        var result = _sut.Map(input);

        Assert.Single(result);
        Assert.IsType<StringValue>(result[0]);
        var sv = (StringValue)result[0];
        Assert.Equal("OtherType/OtherId", sv.Value);
    }

    [Fact]
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

        Assert.Equal(5, result.Count()); //2 line elements + city, country and postalcode.
        foreach (var res in result)
        {
            Assert.IsType<StringValue>(res);
        }
        Assert.Single(result, r => (r as StringValue).Value == "Bruggebouw");
        Assert.Single(result, r => (r as StringValue).Value == "Bos en lommerplein 280");
        Assert.Single(result, r => (r as StringValue).Value == "Netherlands");
        Assert.Single(result, r => (r as StringValue).Value == "Amsterdam");
        Assert.Single(result, r => (r as StringValue).Value == "1055 RW");
    }

    [Fact]
    public void HumanNameMapTest()
    {
        var input = new HumanName();
        input.WithGiven("Pietje").AndFamily("Puk");

        var result = _sut.Map(input);

        Assert.Equal(2, result.Count()); //2 line elements + city, country and postalcode.
        foreach (var res in result)
        {
            Assert.IsType<StringValue>(res);
        }
        Assert.Single(result, r => (r as StringValue).Value == "Pietje");
        Assert.Single(result, r => (r as StringValue).Value == "Puk");
    }

    [Fact]
    public void HumanNameOnlyGivenMapTest()
    {
        var input = new HumanName();
        input.WithGiven("Pietje");

        var result = _sut.Map(input);

        Assert.Single(result); //2 line elements + city, country and postalcode.
        foreach (var res in result)
        {
            Assert.IsType<StringValue>(res);
        }
        Assert.Single(result, r => (r as StringValue).Value == "Pietje");
    }

    private void CheckQuantity(List<Expression> result, decimal? value, string unit, string system, string decimals)
    {
        var nrOfElements = (value.HasValue ? 1 : 0) + new List<String> { unit, system, decimals }.Where(s => s != null).Count();

        Assert.Single(result);
        Assert.IsType<CompositeValue>(result[0]);
        var comp = (CompositeValue)result[0];

        Assert.Equal(nrOfElements, comp.Components.Count());
        Assert.Equal(nrOfElements, comp.Components.Where(c => c.GetType() == typeof(IndexValue)).Count());

        if (value.HasValue)
        {
            var compValue = (IndexValue)comp.Components.Where(c => (c as IndexValue).Name == "value").FirstOrDefault();
            Assert.NotNull(compValue);
            Assert.Single(compValue.Values);
            Assert.IsType<NumberValue>(compValue.Values[0]);
            var numberValue = (NumberValue)compValue.Values[0];
            Assert.Equal(value.Value, numberValue.Value);
        }

        if (unit != null)
        {
            var compUnit = (IndexValue)comp.Components.Where(c => (c as IndexValue).Name == "unit").FirstOrDefault();
            Assert.NotNull(compUnit);
            Assert.Single(compUnit.Values);
            Assert.IsType<StringValue>(compUnit.Values[0]);
            var stringUnit = (StringValue)compUnit.Values[0];
            Assert.Equal(unit, stringUnit.Value);
        }

        if (system != null)
        {
            var compSystem = (IndexValue)comp.Components.Where(c => (c as IndexValue).Name == "system").FirstOrDefault();
            Assert.NotNull(compSystem);
            Assert.Single(compSystem.Values);
            Assert.IsType<StringValue>(compSystem.Values[0]);
            var stringSystem = (StringValue)compSystem.Values[0];
            Assert.Equal(system, stringSystem.Value);
        }

        if (decimals != null)
        {
            var compCode = (IndexValue)comp.Components.Where(c => (c as IndexValue).Name == "decimals").FirstOrDefault();
            Assert.NotNull(compCode);
            Assert.Single(compCode.Values);
            Assert.IsType<StringValue>(compCode.Values[0]);
            var stringCode = (StringValue)compCode.Values[0];
            Assert.Equal(decimals, stringCode.Value);
        }
    }

    [Fact]
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

    [Fact]
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

    [Fact]
    public void CodeMapTest()
    {
        var input = new Code("bla");

        var result = _sut.Map(input);

        Assert.Single(result);
        Assert.IsType<StringValue>(result[0]);

        Assert.Equal("bla", (result[0] as StringValue).Value);
    }

    [Fact]
    public void CodedEnumMapTest()
    {
        var input = new Code<AdministrativeGender>(AdministrativeGender.Male);

        var result = _sut.Map(input);

        Assert.Single(result);
        Assert.IsType<CompositeValue>(result[0]);

        CheckCoding(result[0] as CompositeValue, "male", null, null);
    }

    [Fact]
    public void FhirStringMapTest()
    {
        var input = new FhirString("bla");

        var result = _sut.Map(input);

        Assert.Single(result);
        Assert.IsType<StringValue>(result[0]);

        Assert.Equal("bla", (result[0] as StringValue).Value);
    }
}
