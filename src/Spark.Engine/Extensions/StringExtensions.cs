using System;

namespace Spark.Engine.Extensions
{
    public static class StringExtensions
    {
        public static string FirstUpper(this string input)
        {
            if (String.IsNullOrWhiteSpace(input))
                return input;

            return String.Concat(input.Substring(0, 1).ToUpperInvariant(), input.Remove(0, 1));
        }
    }
}
