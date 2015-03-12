using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core
{
    public static class GeneratorKeyExtensions
    {
        public static IKey NextHistoryKey(this IGenerator generator, IKey key)
        {
            IKey historykey = key;
            historykey.VersionId = generator.NextVersionId(key.TypeName);
            return historykey;
        }

        public static IKey NextKey(this IGenerator generator, string type)
        {
            string id = generator.NextResourceId(type);
            string versionid = generator.NextVersionId(type);
            return Key.CreateLocal(type, id);
        }

        public static IKey NextKey(this IGenerator generator, IKey key)
        {
            string resourceid = generator.NextResourceId(key.TypeName);
            string versionid = generator.NextVersionId(key.TypeName);
            return Key.CreateLocal(key.TypeName, resourceid, versionid);
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
