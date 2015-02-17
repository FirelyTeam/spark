using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core
{
    public static class GeneratorExtensions
    {
        public static string NextResourceId(this IGenerator generator, string type)
        {
            return generator.Next(type);
        }

        public static string NextResourceId(this IGenerator generator, Resource resource)
        {
            string name = resource.GetType().Name;
            return generator.Next(name);
        }

        public static string NextVersionId(this IGenerator generator, Resource resource)
        {
            return generator.NextVersionId(resource.TypeName);
        }

        public static string NextVersionId(this IGenerator generator, string name)
        {
            name = name + "_history";
            return generator.Next(name);
        }

        public static Key NextHistoryKey(this IGenerator generator, Key key)
        {
            Key historykey = key;
            historykey.VersionId = generator.NextVersionId(key.TypeName);
            return historykey;
        }

        public static Key NextKey(this IGenerator generator, string type)
        {
            string id = generator.NextResourceId(type);
            string versionid = generator.NextVersionId(type);
            return new Key(type, id);
        }

        public static Key NextKey(this IGenerator generator, Key key)
        {
            string resourceid = generator.Next(key.TypeName);
            string versionid = generator.NextVersionId(key.TypeName);
            return new Key(key.TypeName, resourceid, versionid);
        }

        public static Key NextKey(this IGenerator generator, Resource resource)
        {
            Key key = resource.ExtractKey();
            return generator.NextKey(key);
        }

    }
}
