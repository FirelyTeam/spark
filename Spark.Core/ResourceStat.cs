using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core
{
    public struct ResourceStat
    {
        public string ResourceName { get; set; }
        public long Count { get; set; }
    }

    public class Stats
    {
        public List<ResourceStat> ResourceStats;
    }
}
