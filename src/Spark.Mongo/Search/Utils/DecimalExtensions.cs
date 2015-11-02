namespace Spark.Search.Mongo
{
    public static class DecimalExtensions
    {
        public static decimal Normalize(this decimal value)
        {
            return value / 1.0000000000000000000000000000M;
        }

        public static decimal? Normalize(this decimal? value)
        {
            if (!value.HasValue)
            {
                return null;
            }
            return Normalize(value.Value);
        }
    }
}