using System;

namespace Spark.Engine.Extensions
{
    public static class DateTimeOffsetExtensions
    {
        public static DateTimeOffset TruncateToMillis(this DateTimeOffset dateTime)
        {
            return dateTime.AddTicks(-(dateTime.Ticks % TimeSpan.TicksPerMillisecond));
        }
    }
}
