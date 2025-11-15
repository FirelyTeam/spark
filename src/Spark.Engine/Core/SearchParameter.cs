using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;
using Spark.Engine.Extensions;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Spark.Engine.Core;

public static class SearchParameterExtensions
{
    private const string X_PATH_SEPARATOR = "/";
    private const string PATH_SEPARATOR = ".";
    private const string GENERAL_PATH_PATTERN = @"(?<chainPart>(?<element>[^{0}\(]+)(?<predicate>\((?<propname>[^=]*)=(?<filterValue>[^\)]*)\))?((?<separator>{0})|(?<endofinput>$)))+";
    private static readonly Regex PATH_PATTERN = new Regex(String.Format(GENERAL_PATH_PATTERN, @"\" + PATH_SEPARATOR));
    
    public static void SetPropertyPath(this SearchParameter searchParameter, string[] paths)
    {
        if (paths != null)
        {
            string[] workingPaths = paths.Select(pp => StripResourceNameFromStart(pp, searchParameter.Base.FirstOrDefault().GetLiteral())).ToArray();
            var xpaths = workingPaths.Select(pp => "//" + PATH_PATTERN.ReplaceGroup(pp, "separator", X_PATH_SEPARATOR));
            searchParameter.Xpath = string.Join(" | ", xpaths);
        }
        else
        {
            searchParameter.Xpath = string.Empty;
        }
    }
    
    private static string StripResourceNameFromStart(string path, string resourceName)
    {
        if (path == null || resourceName == null)
            throw new ArgumentException("path and resourceName are both mandatory.");
        
        return path.StartsWith(resourceName, StringComparison.CurrentCultureIgnoreCase)
            ? path.Remove(0, resourceName.Length + 1)
            : path;
    }
}

public class SearchParameter : IEquatable<SearchParameter>
{
    public string Name { get; set; }
    public string Code { get; set; }
    public VersionIndependentResourceTypesAll[] Base { get; set; }
    public SearchParamType? Type { get; set; }
    public VersionIndependentResourceTypesAll[] Target { get; set; }
    public string Description { get; set; }
    public string Expression { get; set; }
    public string Xpath { get; set; }
    public SearchParamDefinition OriginalDefinition  { get; set; }
       
    public bool Equals(SearchParameter other)
    {
        return string.Equals(Name, other.Name) &&
               string.Equals(Code, other.Code) &&
               Equals(Base, other.Base) &&
               Equals(Type, other.Type) &&
               string.Equals(Description, other.Description) &&
               string.Equals(Xpath, other.Xpath);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((SearchParameter)obj);
    }

    public override int GetHashCode()
    {
        var hashCode = (Name != null ? Name.GetHashCode() : 0);
        hashCode = (hashCode * 397) ^ (Code != null ? Code.GetHashCode() : 0);
        hashCode = (hashCode * 397) ^ (Base != null ? Base.GetHashCode() : 0);
        hashCode = (hashCode * 397) ^ (Type != null ? Type.GetHashCode() : 0);
        hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
        hashCode = (hashCode * 397) ^ (Xpath != null ? Xpath.GetHashCode() : 0);
        return hashCode;
    }
}
