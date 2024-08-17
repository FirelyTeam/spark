/* 
 * Copyright (c) 2015-2018, Furore (info@furore.com) and contributors
 * Copyright (c) 2017-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using System.Collections.Generic;
using Spark.Engine.Core;
using Spark.Core;

namespace Spark.Engine.Extensions
{
    public static class GeneratorKeyExtensions
    {
        public static Key NextHistoryKey(this IIdentityGenerator generator, IKey key)
        {
            Key historykey = key.Clone();
            historykey.VersionId = generator.NextVersionId(key.TypeName, key.ResourceId);
            return historykey;
        }

        public static Key NextKey(this IIdentityGenerator generator, Resource resource)
        {
            string resourceid = generator.NextResourceId(resource);
            Key key = resource.ExtractKey();
            string versionid = generator.NextVersionId(key.TypeName, resourceid);
            return Key.Create(key.TypeName, resourceid, versionid);
        }

        public static void AddHistoryKeys(this IIdentityGenerator generator, List<Entry> entries)
        {
            // PERF: this needs a performance improvement.
            foreach (Entry entry in entries)
            {
                entry.Key = generator.NextHistoryKey(entry.Key);
            }
        }
    }
}
