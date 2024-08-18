/*
 * Copyright (c) 2023-2024, Incendi (info@incendi.no)
 * See the file CONTRIBUTORS for details.
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using System.Linq;

namespace Spark.Engine.Extensions
{
    public static class MetaExtensions
    {
        /// <summary>
        /// Merges the data in source into target.
        /// </summary>
        /// <param name="target">The target of the merge operation.</param>
        /// <param name="source">The source of the merge operation.</param>
        public static void Merge(this Meta target, Meta source)
        {
            var targetProfiles = target.Profile.ToList();
            foreach (var profile in source.Profile)
            {
                if (profile == null)
                    continue;
                if (!targetProfiles.Any(p => profile.Equals(p)))
                    targetProfiles.Add(profile);
            }
            source.Profile = targetProfiles;

            foreach (var securityCoding in source.Security)
            {
                if (target.Security.HasEqualCoding(securityCoding))
                    continue;
                target.Security.Add(securityCoding);
            }

            foreach (var tag in source.Tag)
            {
                if (target.Tag.HasEqualCoding(tag))
                    continue;
                target.Tag.Add(tag);
            }
        }
    }
}
