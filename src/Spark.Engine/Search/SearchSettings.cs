/* 
 * Copyright (c) 2020-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Linq;

namespace Spark.Engine.Search
{
    public class SearchSettings
    {
        /// <summary>
        /// Whether to check missing references. See https://github.com/FirelyTeam/spark/issues/35.
        /// If ths is set to <c>true</c>, then every search that uses reference value would be first checked for
        /// the reference state. If it's is broken (no record was found having the referenced resource type and id) then
        /// the search will return no results.
        ///
        /// Note this is added for backward compatibility only. Default is not to perform any reference state checks.
        /// </summary>
        public bool CheckReferences { get; set; }

        /// <summary>
        /// If <see cref="CheckReferences"/> is <c>true</c>, ensure they are checked only for the resources listed in this property.
        /// If this is <c>null</c>, then reference check will be performed for all properties.
        /// Reference check can be enabled for the whole resource:
        /// <code>
        /// CheckReferencesFor = [ "ResourceName" ]
        /// </code>
        /// or for the particular property or multiple properties:
        /// <code>
        /// CheckReferencesFor = [ "ResourceName.propertyName1", "ResourceName.propertyName2" ]
        /// </code>
        ///
        /// Note this is added for backward compatibility only. Default is not to perform any reference state checks.
        /// </summary>
        public string[] CheckReferencesFor { get; set; }

        /// <param name="resourceType">Not empty string</param>
        /// <param name="paramName">Not empty string</param>
        public bool ShouldSkipReferenceCheck(string resourceType, string paramName)
        {
            if (string.IsNullOrWhiteSpace(resourceType))
            {
                throw new ArgumentNullException(nameof(resourceType));
            }
            if (string.IsNullOrWhiteSpace(paramName))
            {
                throw new ArgumentNullException(nameof(paramName));
            }
            if (!CheckReferences)
            {
                return true;
            }
            if (CheckReferencesFor == null)
            {
                return false;
            }
            return !CheckReferencesFor.Contains(resourceType) &&
                   !CheckReferencesFor.Contains($"{resourceType}.{paramName}");
        }
    }
}
