/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Serialization;

namespace Spark.Engine.Search.Types;

public class NumberValue : ValueExpression
{
    public NumberValue(decimal value)
    {
        Value = value;
    }

    public decimal Value { get; }

    public override string ToString() => PrimitiveTypeConverter.ConvertTo<string>(Value);

    public static NumberValue Parse(string text) => new(PrimitiveTypeConverter.ConvertTo<decimal>(text));
}
