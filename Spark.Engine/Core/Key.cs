using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Core.Auxiliary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core
{
    

    public interface IKey 
    {
        string Base { get; set; }
        string TypeName { get; set; }
        string ResourceId { get; set; }
        string VersionId { get; set; }

    }

    public class Key : IKey
    {
        public string Base { get; set; }
        public string TypeName { get; set; }
        public string ResourceId { get; set; }
        public string VersionId { get; set; }

        public Key() { }

        public Key(string _base, string type, string resourceid, string versionid)
        {
            this.Base = _base;
            this.TypeName = type;
            this.ResourceId = resourceid;
            this.VersionId = versionid;
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

        public static Key Null
        {
            get
            {
                return default(Key);
            }
        }

        public static Key Parse(string _base, string path)
        {
            Key key = new Key();
            key.Base = _base;

            string[] segments = path.Split('/');
            if (segments.Length >= 1) key.Base = segments[0];
            if (segments.Length >= 2) key.ResourceId = segments[1];
            if (segments.Length == 4 && segments[2] == "_history") key.VersionId = segments[3];

            return key;
        }

        public static Key ParseOperationPath(string path)
        {
            return Parse(null, path);
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
