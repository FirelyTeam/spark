using System.Collections.Generic;
using System.Linq;

namespace Spark.Engine.Core
{
    public static class ListExtensions
    {
        public static List<T?> ToNullable<T>(this List<T> list) where T : struct
        {
            return list.Select(i => (T?)i).ToList();
        }
    }
}
