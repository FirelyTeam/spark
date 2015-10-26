using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Engine.Extensions
{
    public static class SearchParameterExtensions
    {
        public static void SetPropertyPath(this SearchParameter searchParameter, string[] path)
        {
            var xpaths = path.Select(pp => "//" + pp.Replace('.', '/'));
            searchParameter.Xpath = String.Join(" | ", xpaths);
        }

        public static string[] GetPropertyPath(this SearchParameter searchParameter)
        {
            var xpaths = searchParameter.Xpath.Split(new string[] { " | " }, StringSplitOptions.None);
            return xpaths.Select(xp => xp.Replace("//", "").Replace("/", ".")).ToArray();
        }
    }
}
