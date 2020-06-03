using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Spark.MetaStore
{
    internal static class Performance
    {
        public static int Measure(Action action)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            action();

            stopwatch.Stop();
            return stopwatch.Elapsed.Seconds;

        }

        public static async Task<int> MeasureAsync(Func<Task> action)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            await action();

            stopwatch.Stop();
            return stopwatch.Elapsed.Seconds;

        }
    }
}