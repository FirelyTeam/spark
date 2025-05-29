/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Serialization;
using Spark.Engine.Search.Support;
using System;

namespace Spark.Engine.Search.Types;

public class QuantityValue : ValueExpression
{
    public QuantityValue(decimal number, string unit)
    {
        Number = number;
        Unit = unit;
    }

    public QuantityValue(decimal number, string ns, string unit)
    {
        Number = number;
        Unit = unit;
        Namespace = ns;
    }

    public decimal Number { get; }

    public string Namespace { get; }

    public string Unit { get; }

    public override string ToString()
    {
        string ns = Namespace ?? string.Empty;
        return
            $"{PrimitiveTypeConverter.ConvertTo<string>(Number)}|{StringValue.EscapeString(ns)}|{StringValue.EscapeString(Unit)}";
    }

    public static QuantityValue Parse(string text)
    {
        if (text == null) throw Error.ArgumentNull("text");

        string[] triple = text.SplitNotEscaped('|');

        if (triple.Length != 3)
            throw Error.Argument("text", "Quantity needs to have three parts separated by '|'");

        if (triple[0] == string.Empty)
            throw new FormatException("Quantity needs to specify a number");

        decimal number = PrimitiveTypeConverter.ConvertTo<decimal>(triple[0]);
        string ns = triple[1] != string.Empty ? StringValue.UnescapeString(triple[1]) : null;

        if (triple[2] == string.Empty)
            throw new FormatException("Quantity needs to specify a unit");

        string unit = StringValue.UnescapeString(triple[2]);

        return new QuantityValue(number, ns, unit);
    }
}
