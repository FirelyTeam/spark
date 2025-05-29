/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Spark.Engine.Search.Support;
using System;

namespace Spark.Engine.Search.Types;

public class ReferenceValue : ValueExpression
{
    public ReferenceValue(string value)
    {
        if (!Uri.IsWellFormedUriString(value, UriKind.Absolute) &&
            !Id.IsValidValue(value))
            throw Error.Argument("text", "Reference is not a valid Id nor a valid absolute Url");

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => StringValue.EscapeString(Value);

    public static ReferenceValue Parse(string text) => new(StringValue.UnescapeString(text));
}
