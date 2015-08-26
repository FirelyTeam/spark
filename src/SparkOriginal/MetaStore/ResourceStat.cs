/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System.Collections.Generic;

namespace Spark.MetaStore
{
    public struct ResourceStat
    {
        public string ResourceName { get; set; }
        public long Count { get; set; }
    }

    public class VmStatistics
    {
        public List<ResourceStat> ResourceStats;
    }
}
