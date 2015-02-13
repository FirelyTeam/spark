using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core
{
    public struct Key
    {
        public string TypeName;
        public string ResourceId;
        public string VersionId;

        public Key(string type, string resourceid)
        {
            this.TypeName = type;
            this.ResourceId = resourceid;
            this.VersionId = null;
        }

        public Key(string type, string resourceid, string versionid)
        {
            this.TypeName = type;
            this.ResourceId = resourceid;
            this.VersionId = versionid;
        }

        public override string ToString()
        {
            string s = string.Format("{0}/{1}", TypeName, ResourceId);
            if (VersionId != null)
            {
                s += string.Format("/{0}/{1}", RestOperation.HISTORY, VersionId);
            }
            return s;
        }

        public Key WithoutVersion()
        {
            Key key = this;
            key.VersionId = null;
            return key;
        }

        public static Key Null
        {
            get
            {
                return default(Key);
            }
        }

        public bool HasVersion
        {
            get
            {
                return string.IsNullOrEmpty(VersionId);
            }
        }
    }

}
