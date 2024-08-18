/* 
 * Copyright (c) 2020-2024, Incendi (info@incendi.no)
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Search;

namespace Spark.Engine.Search
{
    public interface IReferenceNormalizationService
    {
        /// <summary>
        /// <see cref="GetNormalizedReferenceCriteria(Criterium)"/>
        /// </summary>
        /// <param name="originalValue">Can be <c>null</c></param>
        /// <param name="resourceType">Optional</param>
        /// <returns><c>null</c> if the given <c>originalValue</c> is <c>null</c> or can't be used for search for any other reason, normalized value otherwise.</returns>
        ValueExpression GetNormalizedReferenceValue(ValueExpression originalValue, string resourceType);

        /// <summary>
        /// Makes the following transformations on the original criteria operand:
        /// 1. Adds modifier to the reference, if present. Example: <code>?subject:Patient=1001</code> becomes <code>?subject=Patient/1001</code>
        /// 2. Removes local base uri from references. Example: <code>?subject=http://localhost:xyz/fhir/Patient/10014</code> is transformed to
        /// <code>?subject=Patient/10014</code>. Note: external references are not transformed.
        /// </summary>
        /// <returns>Normalized criteria if <c>c</c> is valid, <c>null</c> otherwise</returns>
        Criterium GetNormalizedReferenceCriteria(Criterium c);
    }
}
