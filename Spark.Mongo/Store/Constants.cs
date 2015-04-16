using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Mongo
{

    public static class Collection
    {
        public const string RESOURCE = "resources";
        public const string COUNTERS = "counters";
        public const string SNAPSHOT = "snapshots";
    }

    public static class Field
    {
        // The id field is an actual field in the resource
        public const string RESOURCEID = "id";

        public const string COUNTERVALUE = "last";
        public const string CATEGORY = "category";

        // Meta fields
        public const string PRIMARYKEY = "_id";
        public const string STATE = "@state";
        public const string WHEN = "@when";
        public const string METHOD = "@method"; // Present / Gone
        public const string TYPENAME = "@typename"; // Patient, Organization, etc.
        public const string VERSIONID = "@VersionId"; // The resource versionid is in Resource.Meta. This is a administrative copy

        internal const string TRANSACTION = "@transaction";
        //internal const string TransactionState = "@transstate";
    }

    public static class Value
    {
        public const string CURRENT = "current";
        public const string SUPERCEDED = "superceded";
        internal const string QUEUED = "queued";
        public const string IDPREFIX = "nl.furore.spark.";
        public const string VIDPREFIX = "h";
    }

}
