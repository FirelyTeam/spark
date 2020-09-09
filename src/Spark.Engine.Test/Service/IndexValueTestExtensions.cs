﻿using Spark.Engine.Model;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Engine.Test.Service
{
    public static class IndexValueTestExtensions
    {
        public static IEnumerable<IndexValue> NonInternalValues(this IndexValue root)
        {
            return root.IndexValues().Where(v => !v.Name.StartsWith("internal_"));
        }
    }
}
