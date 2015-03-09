using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core
{
    /// <summary>
    /// Helpes converting variables to human readable text
    /// </summary>
    public static class Language
    {
        
        public static string Since(DateTimeOffset? since)
        {
            return (since != null) ? since.ToString() : "the dawn of man";
        }

        public static string Number<T>(T item, int count)
        {
            string name = item.GetType().ToString();
            switch (count)
            {
                case 0: return string.Format("no {0}s", name);
                case 1: return string.Format("one {0}", name);
                default: return string.Format("{0} {1}s", count, name);
            }
        }
 
    }
}
