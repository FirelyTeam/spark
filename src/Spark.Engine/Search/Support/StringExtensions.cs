/* 
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Collections.Generic;

namespace Spark.Engine.Search.Support;

public static class StringExtensions
{
    public static string[] SplitNotEscaped(this string str, char separator)
    {
        var value = string.Empty;
        var values = new List<string>();
        bool seenEscape = false;

        foreach (var ch in str)
        {
            if (ch == '\\')
            {
                seenEscape = true;
                continue;
            }

            if (ch == separator && !seenEscape)
            {
                values.Add(value);
                value = string.Empty;
                continue;
            }

            if (seenEscape)
            {
                value += '\\';
                seenEscape = false;
            }

            value += ch;
        }

        values.Add(value);

        return values.ToArray();
    }

    public static Tuple<string,string> SplitLeft(this string str, char separator)
    {
        var position = str.IndexOf(separator);

        if (position == -1)
            // Nothing to split.
            return Tuple.Create<string, string>(str, null);

        var key = str[..position];
        var value = str[(position + 1)..];
        return Tuple.Create(key, value);
    }
}
