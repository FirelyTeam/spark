using Hl7.Fhir.Rest;
using Spark.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Service
{
    public class KeyMapper
    {
        Dictionary<Uri, Uri> map = new Dictionary<Uri, Uri>();
        private IGenerator generator;
        private Localhost localhost;

        private const string CID = "cid";

        public bool IsCID(Uri uri)
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

        public KeyMapper(IGenerator generator, Localhost localhost)
        {
            this.generator = generator;
            this.localhost = localhost;
        }

        public void Clear()
        {
            map.Clear();
        }

        public void Map(Uri external, Uri local)
        {
            external = localhost.Absolute(external);
            map.Add(external, local);
        }

        public bool Exists(Uri external)
        {
            external = localhost.Absolute(external);
            return map.ContainsKey(external);
        }

        public Uri Localize(Uri external)
        {
            var identity = new ResourceIdentity(external);
            Uri local = identity.OperationPath;
            this.Map(external, local);
            return local;
        }

        public Uri Remap(Uri external)
        {
            var identity = new ResourceIdentity(external);

            string id = generator.NextKey(identity.Collection);
            Uri local = ResourceIdentity.Build(identity.Collection, id);

            this.Map(external, local);

            return local;
        }

        public Uri Internalize(Uri key)
        {
            return (NeedsRemap(key)) ? Remap(key) : Localize(key);

        }

        public bool NeedsRemap(Uri key)
        {
            // relative uri's are always local to our service. 
            if (!key.IsAbsoluteUri) return false;

            // cid: urls signal placeholder id's and so are never to be considered as true identities
            if (IsCID(key)) return true;

            // Check whether the uri starts with a well-known service path that shares our ID space.
            // Or is an external path that we don't share id's with
            return localhost.HasEndpointFor(key);
        }

        public Uri Get(Uri external)
        {
            return map[external];
        }

        public Uri TryGet(Uri external)
        {
            if (Exists(external))
            {
                return Get(external);
            }
            else
            {
                return external;
            }
        }

        public Uri HistoryKeyFor(Uri key)
        {
            var identity = new ResourceIdentity(key);
            string vid = generator.NextHistoryKey(identity.Collection);
            Uri result = identity.WithVersion(vid);
            return result;
        }
    }
}
