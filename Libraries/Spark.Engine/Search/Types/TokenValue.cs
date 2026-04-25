/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Engine.Search.Support;

namespace Spark.Engine.Search.Types;

public class TokenValue : ValueExpression
{
    public string Namespace { get; set; }

    public string Value { get; set; }

    public bool AnyNamespace { get; set; }

    public override string ToString()
    {
        if (AnyNamespace)
            return StringValue.EscapeString(Value);

        string ns = Namespace ?? string.Empty;
        return $"{StringValue.EscapeString(ns)}|{StringValue.EscapeString(Value)}";
    }

    public static TokenValue Parse(string text)
    {
        if (text == null) throw Error.ArgumentNull("text");

        string[] pair = text.SplitNotEscaped('|');

        if (pair.Length > 2)
            throw Error.Argument("text", "Token cannot have more than two parts separated by '|'");

        bool hasNamespace = pair.Length == 2;

        string pair0 = StringValue.UnescapeString(pair[0]);

        if (!hasNamespace)
            return new TokenValue { Value = pair0, AnyNamespace = true };

        string pair1 = StringValue.UnescapeString(pair[1]);

        if (string.IsNullOrEmpty(pair0))
            return new TokenValue { Value = pair1, AnyNamespace = false };

        return string.IsNullOrEmpty(pair1)
            ? new TokenValue { Namespace = pair0, AnyNamespace = false }
            : new TokenValue { Namespace = pair0, Value = pair1, AnyNamespace = false };
    }
}
