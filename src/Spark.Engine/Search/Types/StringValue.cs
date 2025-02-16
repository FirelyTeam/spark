/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2020-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

namespace Spark.Engine.Search.Types;

public class StringValue : ValueExpression
{
    public StringValue(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public override string ToString() => EscapeString(Value);

    public static StringValue Parse(string text) => new(UnescapeString(text));

    public static string EscapeString(string value)
    {
        if (value == null) return null;

        value = value.Replace(@"\", @"\\");
        value = value.Replace(@"$", @"\$");
        value = value.Replace(@",", @"\,");
        value = value.Replace(@"|", @"\|");

        return value;
    }

    public static string UnescapeString(string value)
    {
        if (value == null) return null;

        value = value.Replace(@"\|", @"|");
        value = value.Replace(@"\,", @",");
        value = value.Replace(@"\$", @"$");
        value = value.Replace(@"\\", @"\");

        return value;
    }
}
