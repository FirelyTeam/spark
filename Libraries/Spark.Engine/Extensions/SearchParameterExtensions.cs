/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SearchParameter = Spark.Engine.Model.SearchParameter;

namespace Spark.Engine.Extensions;

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

    /// <summary>
    /// Returns true if the search parameter is one of the types
    /// that support comparison prefix operators: Number, Date or
    /// Quantity.
    /// See https://www.hl7.org/fhir/stu3/search.html#prefix for
    /// more information.
    /// </summary>
    internal static bool CanHaveOperatorPrefix(
        this IReadOnlyList<SearchParameter> searchParameters,
        string resourceType,
        string name)
    {
        var sp = searchParameters.FirstOrDefault(
            p => (p.Resource == resourceType || p.Resource == "Resource")
                 && p.Name == name);
        return sp != null
               && (sp.Type == SearchParamType.Number
                   || sp.Type == SearchParamType.Date
                   || sp.Type == SearchParamType.Quantity);
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
