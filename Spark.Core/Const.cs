using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core
{
    public static class FhirRestOp
    {
        public const string SNAPSHOT = "_snapshot";
    }

    public static class FhirHeader
    {
        public const string CATEGORY = "Category";
    }

    public static class FhirParameter
    {
        public const string SNAPSHOT_ID = "id";
        public const string SNAPSHOT_INDEX = "start";
        public const string COUNT = "_count";
        public const string SINCE = "_since";
    }
}
