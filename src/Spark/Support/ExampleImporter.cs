/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */
using System;
using System.Collections.Generic;
using Hl7.Fhir.Model;
using Spark.Embedded;
using Spark.Engine.Extensions;

namespace Spark.Support
{
    public static class Examples
    {
        //List<Interaction> entries = new List<Interaction>();

        public static IEnumerable<Resource> ImportEmbeddedZip()
        {
            return Resources.ExamplesZip.ExtractResourcesFromZip();
        }

        public static IEnumerable<Resource> LimitPerType(this IEnumerable<Resource> resources, int amount)
        {
            Dictionary<string, int> counters = new Dictionary<string, int>();
            foreach(Resource r in resources)
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
                return ++ dictionary[key];
            }
            else
            {
                dictionary.Add(key, 1);
                return 1;
            }
        }

        public static Bundle ToBundle(this IEnumerable<Resource> resources, Uri _base)
        {
            Bundle bundle = new Bundle();
            bundle.Base = _base.ToString();
            foreach(Resource resource in resources)
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
