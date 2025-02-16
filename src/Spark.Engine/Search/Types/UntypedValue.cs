/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;

namespace Spark.Engine.Search.Types;

public class UntypedValue : ValueExpression
{
    public UntypedValue(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;

    public NumberValue AsNumberValue() => NumberValue.Parse(Value);

    public DateValue AsDateValue() => DateValue.Parse(Value);

    public FhirDateTime AsDateTimeValue() => new(Value);

    public StringValue AsStringValue() => StringValue.Parse(Value);

    public TokenValue AsTokenValue() => TokenValue.Parse(Value);

    public QuantityValue AsQuantityValue() => QuantityValue.Parse(Value);

    public ReferenceValue AsReferenceValue() => ReferenceValue.Parse(Value);
}
