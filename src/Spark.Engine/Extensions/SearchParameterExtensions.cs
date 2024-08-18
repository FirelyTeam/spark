/* 
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Utility;
using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Hl7.Fhir.Rest;

namespace Spark.Engine.Extensions
{
    public static class SearchParameterExtensions
    {
        private const string XPathSeparator = "/";
        private const string PathSeparator = ".";
        private const string GeneralPathPattern = @"(?<chainPart>(?<element>[^{0}\(]+)(?<predicate>\((?<propname>[^=]*)=(?<filterValue>[^\)]*)\))?((?<separator>{0})|(?<endofinput>$)))+";
        public static Regex XPathPattern = new Regex(String.Format(@"(?<root>^//)" + GeneralPathPattern, XPathSeparator));
        public static Regex PathPattern = new Regex(String.Format(GeneralPathPattern, @"\" + PathSeparator));

        public static void SetPropertyPath(this SearchParameter searchParameter, string[] paths)
        {
            string[] workingPaths;
            if (paths != null)
            {
                // TODO: Added FirstOrDefault to searchParameter.Base.GetLiteral() could possibly generate a bug

                //A searchparameter always has a Resource as focus, so we don't need the name of the resource to be at the start of the Path.
                //See also: https://github.com/ewoutkramer/fhirpath/blob/master/fhirpath.md
                workingPaths = paths.Select(pp => StripResourceNameFromStart(pp, searchParameter.Base.FirstOrDefault().GetLiteral())).ToArray();
                var xpaths = workingPaths.Select(pp => "//" + PathPattern.ReplaceGroup(pp, "separator", XPathSeparator));
                searchParameter.Xpath = string.Join(" | ", xpaths);
            }
            else
            {
                searchParameter.Xpath = string.Empty;
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
                return xpaths.Select(xp => XPathPattern.ReplaceGroups(xp, new Dictionary<string, string>{ { "separator", PathSeparator},{ "root", String.Empty} })).ToArray();
            }
            else
            {
                return new string[] { };
            }
        }

        public static ModelInfo.SearchParamDefinition GetOriginalDefinition(this SearchParameter searchParameter)
        {
            return searchParameter.Annotation<ModelInfo.SearchParamDefinition>();
        }

        public static void SetOriginalDefinition(this SearchParameter searchParameter, ModelInfo.SearchParamDefinition definition)
        {
            searchParameter.AddAnnotation(definition);
        }

        public static SearchParams AddAll(this SearchParams self, List<Tuple<string, string>> @params)
        {
            foreach (var (item1, item2) in @params)
            {
                self.Add(item1, item2);
            }
            return self;
        }
    }
}
