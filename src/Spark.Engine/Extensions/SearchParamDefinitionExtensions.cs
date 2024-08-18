/* 
 * Copyright (c) 2019-2024, Incendi (info@incendi.no)
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

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
        internal static bool CanHaveOperatorPrefix(this List<SearchParamDefinition> searchParamDefinitions, string resourceType, string name)
        {
            SearchParamDefinition searchParamDefinition = searchParamDefinitions.Find(p => (p.Resource == resourceType || p.Resource == nameof(Resource)) && p.Name == name);
            return searchParamDefinition != null && (searchParamDefinition.Type == SearchParamType.Number
                || searchParamDefinition.Type == SearchParamType.Date
                || searchParamDefinition.Type == SearchParamType.Quantity);
        }
    }
}
