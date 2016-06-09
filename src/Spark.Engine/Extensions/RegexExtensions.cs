using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
                return ReplaceNamedGroups(m, input, replacements);
            });
        }

        private static string ReplaceNamedGroups(Match m, string input, Dictionary<string, string> replacements)
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
