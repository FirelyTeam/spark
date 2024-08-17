/* 
 * Copyright (c) 2015-2018, Furore (info@furore.com) and contributors
 * Copyright (c) 2021-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Spark.Search.Support
{
    public static class StringExtensions
    {
        public static string[] SplitNotInQuotes(this string value, char separator)
        {
            var parts = Regex.Split(value, separator + "(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)")
                                .Select(s => s.Trim());
                               
            return parts.ToArray<string>();
        }

        public static string[] SplitNotEscaped(this string value, char separator)
        {
            string word = string.Empty;
            List<string> result = new List<string>();
            bool seenEscape = false;

            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '\\')
                {
                    seenEscape = true;
                    continue;
                }
               
                if (value[i] == separator && !seenEscape)
                {
                    result.Add(word);
                    word = string.Empty;
                    continue;
                }

                if (seenEscape)
                {
                    word += '\\';
                    seenEscape = false;
                }

                word += value[i];
            }

            result.Add(word);

            return result.ToArray<string>();
        }

        public static Tuple<string,string> SplitLeft(this string text, char separator)
        {
            var pos = text.IndexOf(separator);

            if (pos == -1)
                return Tuple.Create(text, (string)null);     // Nothing to split
            else
            {
                var key = text.Substring(0, pos);
                var value = text.Substring(pos + 1);

                return Tuple.Create(key, value);
            }
        }
    }
}
