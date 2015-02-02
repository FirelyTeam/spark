using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Store
{


    public static class Field
    {
        public const string RESOURCEID = "id";
        public const string VERSIONID = "VersionId";

        public const string COUNTERVALUE = "last";
        public const string CATEGORY = "category";
        public const string KEYPREFIX = "spark";

        // Meta fields
        public const string RECORDID = "_id";
        public const string STATE = "@state";
        public const string VERSIONDATE = "@versionDate";
        public const string OPERATION = "@operation"; // CREATE, UPDATE, DELETE
        public const string TYPENAME = "@typename"; // Patient, Organization, etc.

        internal const string TRANSACTION = "@transaction";
        //internal const string TransactionState = "@transstate";
    }

    public static class Value
    {
        public const string CURRENT = "current";
        public const string SUPERCEDED = "superceded";
        internal const string QUEUED = "queued";
    }

}
