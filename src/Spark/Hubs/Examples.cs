using System;
using System.Collections.Generic;
using System.IO;
using Hl7.Fhir.Model;
using Spark.Engine.Extensions;


namespace Spark.Import
{

    public static class Examples
    {
        public static IEnumerable<Resource> ImportEmbeddedZip(string path)
        {
            return GetPathAsBytes(path).ExtractResourcesFromZip();
        }

        public static byte[] GetPathAsBytes(string path)
        {
            return File.ReadAllBytes(path);
        }

        public static IEnumerable<Resource> LimitPerType(this IEnumerable<Resource> resources, int amount)
        {
            Dictionary<string, int> counters = new Dictionary<string, int>();
            foreach (Resource r in resources)
            {
                if (counters.Inc(r.TypeName) <= amount)
                {
                    yield return r;
                }
            }
        }

        public static int Inc<T>(this Dictionary<T, int> dictionary, T key)
        {
            if (dictionary.ContainsKey(key))
            {
                return ++dictionary[key];
            }
            else
            {
                dictionary.Add(key, 1);
                return 1;
            }
        }

        [Obsolete("Use method with signature ToBundle(this IEnumerable<Resource>)")]
        public static Bundle ToBundle(this IEnumerable<Resource> resources, Uri _base)
        {
            return ToBundle(resources);
        }

        public static Bundle ToBundle(this IEnumerable<Resource> resources)
        {
            Bundle bundle = new Bundle();
            foreach (Resource resource in resources)
            {
                // Make sure that resources without id's are posted.
                if (resource.Id != null)
                {
                    bundle.Append(Bundle.HTTPVerb.PUT, resource);
                }
                else
                {
                    bundle.Append(Bundle.HTTPVerb.POST, resource);
                }
            }

            return bundle;
        }

    }
}