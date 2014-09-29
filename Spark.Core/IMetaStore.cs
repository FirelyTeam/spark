using System;

namespace Spark.Core
{
    public interface IMetaStore
    {
        System.Collections.Generic.List<ResourceStat> GetResourceStats();
    }
}
