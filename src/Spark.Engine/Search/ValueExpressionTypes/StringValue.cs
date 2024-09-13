/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2020-2024, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

namespace Spark.Search;

public class StringValue : ValueExpression
{
    public string Value { get; private set; }

    public StringValue(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return EscapeString(Value);
    }

    public static StringValue Parse(string text)
    {
        return new StringValue(UnescapeString(text));
    }

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