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
        public string Base;
        public string TypeName;
        public string ResourceId;
        public string VersionId;

        public Key(string _base, string type, string resourceid, string versionid)
        {
            this.Base = _base;
            this.TypeName = type;
            this.ResourceId = resourceid;
            this.VersionId = versionid;
        }

        public bool IsLocal
        {
            get
            {
                return Base == null;
            }
        }

        public static Key CreateLocal(string type)
        {
            return new Key(null, type, null, null);
        }

        public static Key CreateLocal(string type, string resourceid)
        {
            return new Key(null, type, resourceid, null);
        }

        public static Key CreateLocal(string type, string resourceid, string versionid)
        {
            return new Key(null, type, resourceid, versionid);
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

        public bool HasVersionId
        {
            get
            {
                return !string.IsNullOrEmpty(this.VersionId);
            }
        }

        public bool HasResourceId
        {
            get
            {
                return !string.IsNullOrEmpty(this.ResourceId);
            }
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

    }

}
