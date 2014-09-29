/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

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
