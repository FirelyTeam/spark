/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Hl7.Fhir.Model;
using System.Net.Http.Headers;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Spark.Config;

namespace Spark.Core
{
    
    // todo: DSTU2
    /* 
    public static class TagHelper
    {
        public static List<Tag> GetFhirTags(this HttpHeaders headers)
        {
            IEnumerable<string> tagstrings;
            List<Tag> tags = new List<Tag>();
            
            if (headers.TryGetValues(FhirHeader.CATEGORY, out tagstrings))
            {
                foreach (string tagstring in tagstrings)
                {
                    tags.AddRange(HttpUtil.ParseCategoryHeader(tagstring));
                }
            }
            return tags;
        }

        public static void SetFhirTags(this HttpHeaders headers, IEnumerable<Tag> tags)
        {
            string tagstring = HttpUtil.BuildCategoryHeader(tags);
            headers.Add(FhirHeader.CATEGORY, tagstring);
        }
    
        public static IEnumerable<Tag> Affix(this IEnumerable<Tag> tags, IEnumerable<Tag> other)
        {
            // Union works with equality [http://www.healthintersections.com.au/?p=1941]
            // the other should overwrite the existing tags, so the union starts with other.
            
            IEnumerable<Tag> original = tags.Except(other);
            return other.Concat(original).FilterOnFhirSchemes();

            //return other.Union(tags).FilterOnFhirSchemes();
        }

        public static IEnumerable<Tag> AffixTags(Resource entry, Resource other)
        {
            Hl7.Fhir.Model.
            IEnumerable<Tag> entryTags = entry.Tags ?? Enumerable.Empty<Tag>();
            IEnumerable<Tag> otherTags = other.Tags ?? Enumerable.Empty<Tag>();
            return Affix(entryTags, otherTags);
        }

        public static void AffixTags(this BundleEntry entry, IEnumerable<Tag> tags)
        {
            entry.Tags = Affix(entry.Tags, tags).ToList();
        }
    }
    */
}