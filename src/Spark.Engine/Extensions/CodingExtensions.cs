/*
 * Copyright (c) 2023-2024, Incendi (info@incendi.no)
 * See the file CONTRIBUTORS for details.
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Engine.Extensions
{
    public static class CodingExtensions
    {
        /// <summary>
        /// Compares this <see cref="Coding"/> instance against other, returns true if the two have identical System and
        /// Code, otherwise false.
        /// </summary>
        /// <param name="coding">This <see cref="Coding"/> instance.</param>
        /// <param name="other">The <see cref="Coding"/> instance to compare this against.</param>
        /// <returns></returns>
        public static bool AreEqual(this Coding coding, Coding other)
        {
            return coding.System == other.System && coding.Code == other.Code;
        }

        /// <summary>
        /// Compares this list of <see cref="Coding"/> Coding instances against the <see cref="Coding"/> instance other,
        /// returns true if at least one <see cref="Coding"/> instance have identical System and Code to other, otherwise false.
        /// </summary>
        /// <param name="sources">This list of <see cref="Coding"/> instances.</param>
        /// <param name="other">The <see cref="Coding"/> instance to compare this list against.</param>
        /// <returns></returns>
        public static bool HasEqualCoding(this IEnumerable<Coding> sources, Coding other)
        {
            return sources.Any(source => AreEqual(source, other));
        }
    }
}
