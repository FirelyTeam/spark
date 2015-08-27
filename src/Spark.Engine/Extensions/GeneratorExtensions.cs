using Hl7.Fhir.Model;
using Spark.Core;

namespace Spark.Engine.Extensions
{
    public static class Format
    {
        public static string RESOURCEID = "spark{0}";
        public static string VERSIONID = "spark{0}";
    }

    public static class GeneratorExtensions
    {
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
