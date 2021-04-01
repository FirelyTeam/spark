namespace Spark.Engine.Extensions
{
    public static class StringExtensions
    {
        public static string FirstUpper(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            return string.Concat(input.Substring(0, 1).ToUpperInvariant(), input.Remove(0, 1));
        }
    }
}
