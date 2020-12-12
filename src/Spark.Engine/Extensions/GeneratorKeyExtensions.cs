using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Core;

namespace Spark.Engine.Extensions
{
    using System.Threading.Tasks;

    public static class GeneratorKeyExtensions
    {
        public static async Task<Key> NextHistoryKey(this IGenerator generator, IKey key)
        {
            Key historykey = key.Clone();
            historykey.VersionId = await generator.NextVersionId(key.TypeName, key.ResourceId).ConfigureAwait(false);
            return historykey;
        }

        public static async Task<Key> NextKey(this IGenerator generator, Resource resource)
        {
            string resourceid = await generator.NextResourceId(resource).ConfigureAwait(false);
            Key key = resource.ExtractKey();
            string versionid = await generator.NextVersionId(key.TypeName, resourceid).ConfigureAwait(false);
            return Key.Create(key.TypeName, resourceid, versionid);
        }

        //public static void AddHistoryKeys(this IGenerator generator, List<Entry> entries)
        //{
        //    // PERF: this needs a performance improvement.
        //    foreach (Entry entry in entries)
        //    {
        //        entry.Key = generator.NextHistoryKey(entry.Key);
        //    }
        //}
    }
}
