using Hl7.Fhir.Rest;
using Spark.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Service
{
    public static class Key
    {
        public const string CID = "cid";

        public static bool IsCID(Uri uri)
        {
            if (uri.IsAbsoluteUri)
            {
                return (uri.Scheme.ToLower() == CID);
            }
            else
            {
                return false;
            }
        }

        public static Uri GetOperationpath(this Uri uri)
        {
            var identity = new ResourceIdentity(uri);
            Uri key = identity.OperationPath;
            return key;
        }
        /*
        public Uri Localize(Uri external)
        {
            var identity = new ResourceIdentity(external);
            Uri local = identity.OperationPath;
            this.Map(external, local);
            return local;
        }
        */

        public static Uri HistoryKeyFor(this IGenerator generator, Uri key)
        {
            var identity = new ResourceIdentity(key);
            string vid = generator.NextHistoryKey(identity.Collection);
            Uri result = identity.WithVersion(vid);
            return result;
        }

        public static bool KeyNeedsRemap(this Localhost localhost, Uri key)
        {
            // relative uri's are always local to our service. 
            if (!key.IsAbsoluteUri) return false;

            // cid: urls signal placeholder id's and so are never to be considered as true identities
            if (IsCID(key)) return true;

            // Check whether the uri starts with a well-known service path that shares our ID space.
            // Or is an external path that we don't share id's with
            return localhost.HasEndpointFor(key);
        }


        
    }
}
