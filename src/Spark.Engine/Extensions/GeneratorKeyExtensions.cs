using Hl7.Fhir.Model;
using System.Collections.Generic;
using Spark.Engine.Core;
using Spark.Core;

namespace Spark.Engine.Extensions
{
    public static class GeneratorKeyExtensions
    {
        public static Key NextHistoryKey(this IGenerator generator, IKey key)
        {
            Key historykey = key.Clone();
            historykey.VersionId = generator.NextVersionId(key.TypeName);
            return historykey;
        }

        public static Key NextKey(this IGenerator generator, string type)
        {
            string id = generator.NextResourceId(type);
            string versionid = generator.NextVersionId(type);
            return Key.Create(type, id);
        }

        public static Key NextKey(this IGenerator generator, IKey key)
        {
            string resourceid = generator.NextResourceId(key.TypeName);
            string versionid = generator.NextVersionId(key.TypeName);
            return Key.Create(key.TypeName, resourceid, versionid);
        }

        public static IKey NextKey(this IGenerator generator, Resource resource)
        {
            IKey key = resource.ExtractKey();
            return generator.NextKey(key);
        }

        public static void AddHistoryKeys(this IGenerator generator, List<Interaction> interactions)
        {
            // PERF: this needs a performance improvement.
            foreach (Interaction interaction in interactions)
            {
                interaction.Key = generator.NextHistoryKey(interaction.Key);
            }
        }
    }
}
