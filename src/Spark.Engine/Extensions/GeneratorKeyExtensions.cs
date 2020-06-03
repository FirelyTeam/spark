using Hl7.Fhir.Model;
using System.Collections.Generic;
using Spark.Engine.Core;
using Spark.Core;
using System.Threading.Tasks;

namespace Spark.Engine.Extensions
{
    public static class GeneratorKeyExtensions
    {
        public static async Task<Key> NextHistoryKey(this IGenerator generator, IKey key)
        {
            Key historykey = key.Clone();
            historykey.VersionId = await generator.NextVersionId(key.TypeName, key.ResourceId);
            return historykey;
        }

        public static async Task<Key> NextKey(this IGenerator generator, Resource resource)
        {
            string resourceid = await generator.NextResourceId(resource);
            Key key = resource.ExtractKey();
            string versionid = await generator.NextVersionId(key.TypeName, resourceid);
            return Key.Create(key.TypeName, resourceid, versionid);
        }

        public static async System.Threading.Tasks.Task AddHistoryKeys(this IGenerator generator, List<Entry> entries)
        {
            // PERF: this needs a performance improvement.
            foreach (Entry entry in entries)
            {
                entry.Key = await generator.NextHistoryKey(entry.Key);
            }
        }
    }
}
