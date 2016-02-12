using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Engine.Extensions
{
    public static class SearchParameterExtensions
    {
        public static void SetPropertyPath(this SearchParameter searchParameter, string[] paths)
        {
            string[] workingPaths;
            if (paths != null)
            {
                //A searchparameter always has a Resource as focus, so we don't need the name of the resource to be at the start of the Path.
                //See also: https://github.com/ewoutkramer/fhirpath/blob/master/fhirpath.md
                workingPaths = paths.Select<string, string>(pp => StripResourceNameFromStart(pp, searchParameter.Base.GetLiteral())).ToArray();
                var xpaths = workingPaths.Select(pp => "//" + pp.Replace('.', '/'));
                searchParameter.Xpath = String.Join(" | ", xpaths);
            }
            else
            {
                searchParameter.Xpath = String.Empty;
                //Null is not an error, for example Composite parameters don't have a path.
            }
        }

        private static string StripResourceNameFromStart(string path, string resourceName)
        {
            if (path == null || resourceName == null)
            {
                throw new ArgumentException("path and resourceName are both mandatory.");
            }
            if (path.StartsWith(resourceName, StringComparison.CurrentCultureIgnoreCase))
            {
                //Path is like "Patient.birthdate", but "Patient." is superfluous. Ignore it.
                return path.Remove(0, resourceName.Length + 1); 
            }
            else
            {
                return path;
            }
        }

        public static string[] GetPropertyPath(this SearchParameter searchParameter)
        {
            if (searchParameter.Xpath != null)
            {
                var xpaths = searchParameter.Xpath.Split(new string[] { " | " }, StringSplitOptions.None);
                return xpaths.Select(xp => xp.Replace("//", "").Replace("/", ".")).ToArray();
            }
            else
            {
                return new string[] { };
            }
        }

        public static ModelInfo.SearchParamDefinition GetOriginalDefinition(this SearchParameter searchParameter)
        {
            object spDefObject;
            searchParameter.UserData.TryGetValue("original_definition", out spDefObject);

            if (spDefObject != null)
            {
                return (ModelInfo.SearchParamDefinition)spDefObject;
            }
            return null;
        }

        public static void SetOriginalDefinition(this SearchParameter searchParameter, ModelInfo.SearchParamDefinition definition)
        {
            searchParameter.UserData.Add("original_definition", definition);
        }
    }
}
