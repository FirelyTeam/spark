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
    public static class HttpFhirTagExtensions
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
    }
}