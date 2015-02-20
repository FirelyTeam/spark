using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core
{
    public static class Format
    {
        public static string RESOURCEID = "spark{0}";
        public static string VERSIONID = "spark{0}";
    }

    public static class GeneratorExtensions
    {
        /*public static string NextResourceId(this IGenerator generator, string type)
        {
            string id = generator.Next(type);
            return string.Format(Format.RESOURCEID, id);
        }

        public static string NextVersionId(this IGenerator generator, string name)
        {
            name = name + "_history";
            string id = generator.Next(name);
            return string.Format(Format.VERSIONID, id);
        }
        */

        public static string NextResourceId(this IGenerator generator, Resource resource)
        {
            string name = resource.TypeName;
            return generator.NextResourceId(name);
        }

        public static string NextVersionId(this IGenerator generator, Resource resource)
        {
            
            return generator.NextVersionId(resource.TypeName);
        }

    }

    
}
