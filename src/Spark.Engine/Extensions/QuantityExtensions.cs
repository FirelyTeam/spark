/* 
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Fhir.Metrics;
using FM = Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Spark.Search;
using Spark.Engine.Model;

namespace Spark.Engine.Extensions;

public static class QuantityExtensions
{
    public static string UcumUriString = "http://unitsofmeasure.org";
    public static SystemOfUnits System = UCUM.Load();

    public static Quantity ToUnitsOfMeasureQuantity(this FM.Quantity input)
    {
        Metric metric = (input.Code != null) ? System.Metric(input.Code) : new Metric(new List<Metric.Axis>());
        Exponential value = input.Value ?? 1; //todo: is this assumption correct?
        return new Quantity(value, metric);
    }
    public static FM.Quantity ToFhirModelQuantity(this Quantity input)
    {
        FM.Quantity output = new FM.Quantity
        {
            Value = (decimal)input.Value,
            Code = input.Metric.ToString()
        };
        output.Unit = output.Code;
        output.System = UcumUriString;
        return output;
    }

    public static Expression ToExpression(this Quantity quantity)
    {
        quantity = quantity.Canonical();
        string searchable = quantity.LeftSearchableString();

        var values = new List<ValueExpression>
        {
            new IndexValue("system", new StringValue(UcumUriString)),
            new IndexValue("value", new NumberValue(quantity.Value.ToDecimal())),
            new IndexValue("decimals", new StringValue(searchable)),
            new IndexValue("unit", new StringValue(quantity.Metric.ToString()))
        };

        return new CompositeValue(values);
    }

    public static Expression NonUcumIndexedExpression(this FM.Quantity quantity)
    {
        var values = new List<ValueExpression>();
        if (quantity.System != null)
            values.Add(new IndexValue("system", new StringValue(quantity.System)));

        if (quantity.Unit != null)
            values.Add(new IndexValue("unit", new StringValue(quantity.Unit)));

        if (quantity.Value.HasValue)
            values.Add(new IndexValue("value", new NumberValue(quantity.Value.Value)));

        if (values.Any())
            return new CompositeValue(values);

        return null;
    }

    public static Expression ToExpression(this FM.Quantity quantity)
    {
        if (quantity.IsUcum())
        {
            Quantity q = quantity.ToUnitsOfMeasureQuantity();
            return q.ToExpression();
        }
        else return quantity.NonUcumIndexedExpression();
    }

    public static bool IsUcum(this FM.Quantity quantity)
    {
        if (quantity.System == null)
            return false;

        return new Uri(UcumUriString).IsBaseOf(new Uri(quantity.System));
    }

    public static Quantity Canonical(this Quantity input)
    {
        return input.Metric.Symbols switch
        {
            // TODO: Conversion of Celsius to its base unit Kelvin fails using the method SystemOfUnits::Canoncial
            // Waiting for feedback on issue: https://github.com/FirelyTeam/Fhir.Metrics/issues/7
            "Cel" => new Quantity(input.Value + 273.15m, System.Metric("K")),
            "[degF]" => new Quantity((input.Value - 32)/1.8m + 273.15m, System.Metric("K")),
            _ => System.Canonical(input),
        };
    }

    public static FM.Quantity Canonical(this FM.Quantity input)
    {
        if (IsUcum(input))
        {
            Quantity quantity = input.ToUnitsOfMeasureQuantity();
            quantity = quantity.Canonical();
            return quantity.ToFhirModelQuantity();
        }
        else return input;
    }

    public static string ValueAsSearchableString(this FM.Quantity quantity)
    {
        Quantity q = quantity.ToUnitsOfMeasureQuantity();
        return q.LeftSearchableString();
    }

    public static string SearchableString(this Quantity quantity)
    {
        return quantity.LeftSearchableString(); // extension access
    }
}
