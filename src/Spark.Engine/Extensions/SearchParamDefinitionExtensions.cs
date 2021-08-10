using Hl7.Fhir.Model;
using System.Collections.Generic;
using static Hl7.Fhir.Model.ModelInfo;

namespace Spark.Engine.Extensions
{
    internal static class SearchParamDefinitionExtensions
    {
        /// <summary>
        /// Returns true if the search parameter is one of the following types: Number, Date or Quantity.
        /// See https://www.hl7.org/fhir/stu3/search.html#prefix for more information.
        /// </summary>
        /// <param name="searchParamDefinitions">
        /// A List of <see cref="SearchParamDefinition"/>, since this is an extension this is usually a reference 
        /// to ModelInfo.SearcParameters.
        /// </param>
        /// <param name="name">A <see cref="string"/> representing the name of the search parameter.</param>
        /// <returns>Returns true if the search parameter is of type Number, Date or Quanity, otherwise false.</returns>
        internal static bool CanHaveOperatorPrefix(this List<SearchParamDefinition> searchParamDefinitions, string name)
        {
            SearchParamDefinition searchParamDefinition = searchParamDefinitions.Find(p => p.Name == name);
            return searchParamDefinition != null && (searchParamDefinition.Type == SearchParamType.Number
                || searchParamDefinition.Type == SearchParamType.Date
                || searchParamDefinition.Type == SearchParamType.Quantity);
        }
        
        /// <summary>
        /// Returns true if the search parameter is one of the following types: Number, Date or Quantity.
        /// See https://www.hl7.org/fhir/stu3/search.html#prefix for more information.
        /// </summary>
        /// <param name="searchParamDefinitions">
        /// A List of <see cref="SearchParamDefinition"/>, since this is an extension this is usually a reference 
        /// to ModelInfo.SearcParameters.
        /// </param>
        /// <param name="resourceType">A <see cref="string"/> representing the resource type of the search parameter</param>
        /// <param name="name">A <see cref="string"/> representing the name of the search parameter.</param>
        /// <returns>Returns true if the search parameter is of type Number, Date or Quanity, otherwise false.</returns>
        internal static bool CanHaveOperatorPrefix(this List<SearchParamDefinition> searchParamDefinitions, string resourceType, string name)
        {
            // If this is a global SearchParameter then do not include the resource type.
            SearchParamDefinition searchParamDefinition = IsGlobalSearchParameter(name)
                ? searchParamDefinitions.Find(p => p.Name == name)
                : searchParamDefinitions.Find(p => p.Resource == resourceType && p.Name == name);
            return searchParamDefinition != null && (searchParamDefinition.Type == SearchParamType.Number
                                                     || searchParamDefinition.Type == SearchParamType.Date
                                                     || searchParamDefinition.Type == SearchParamType.Quantity);
        }

        private static bool IsGlobalSearchParameter(string name)
        {
            return name.IndexOf('_') == 0;
        }
    }
}
