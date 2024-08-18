/* 
 * Copyright (c) 2016-2018, Firely (info@fire.ly) and contributors
 * Copyright (c) 2021-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Spark.Engine.Extensions
{
    public static class RegexExtensions
    {
        public static string ReplaceGroup(this Regex regex, string input, string groupName, string replacement)
        {
            return ReplaceGroups(regex, input, new Dictionary<string, string> { { groupName, replacement } });
        }

        public static string ReplaceGroups(this Regex regex, string input, Dictionary<string, string> replacements)
        {
            return regex.Replace(input, m =>
            {
                return ReplaceNamedGroups(m, replacements);
            });
        }

        private static string ReplaceNamedGroups(Match m, Dictionary<string, string> replacements)
        {
            string result = m.Value;
            foreach (var replacement in replacements)
            {
                var groupName = replacement.Key;
                var replaceWith = replacement.Value;
                foreach (Capture cap in m.Groups[groupName]?.Captures)
                {
                    result = result.Remove(cap.Index - m.Index, cap.Length);
                    result = result.Insert(cap.Index - m.Index, replaceWith);
                }
            }
            return result;
        }
    }
}
