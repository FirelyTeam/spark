using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core
{

    public interface IGenerator
    {
        string NextKey(string name);
    }

    public static class GeneratorExtensions
    {
        public static string NextKey(this IGenerator generator, Resource resource)
        {
            string name = resource.GetType().Name;
            return generator.NextKey(name);
        }

        public static string NextHistoryKey(this IGenerator generator, Resource resource)
        {
            string name = resource.GetType().Name + "_history";
            return generator.NextKey(name);
        }

        public static string NextHistoryKey(this IGenerator generator, string name)
        {
            name = name + "_history";
            return generator.NextKey(name);
        }
    }

}
