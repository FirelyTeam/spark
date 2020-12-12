/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;

namespace Spark.Engine.Extensions
{

    public static class TagHelper
    {
        //public static List<Tag> GetFhirTags(this HttpHeaders headers)
        //{
        //    IEnumerable<string> tagstrings;
        //    List<Tag> tags = new List<Tag>();
            
        //    if (headers.TryGetValues(FhirHeader.CATEGORY, out tagstrings))
        //    {
        //        foreach (string tagstring in tagstrings)
        //        {
        //            tags.AddRange(HttpUtil.ParseCategoryHeader(tagstring));
        //        }
        //    }
        //    return tags;
        //}

        //public static void SetFhirTags(this HttpHeaders headers, IEnumerable<Tag> tags)
        //{
        //    string tagstring = HttpUtil.BuildCategoryHeader(tags);
        //    headers.Add(FhirHeader.CATEGORY, tagstring);
        //}
    
        public static bool EqualTag(Coding coding, Coding other)
        {
            return (coding.System == other.System);
        }

        public static bool HasTag(this IEnumerable<Coding> tags, Coding tag)
        {
            return tags.Any(t => EqualTag(t, tag));
        }

        public static IEnumerable<Coding> AffixTags(this IEnumerable<Coding> target, IEnumerable<Coding> source)
        {
            // Union works with equality [http://www.healthintersections.com.au/?p=1941]
            // the source should overwrite the existing target tags

            foreach(Coding s in source)
            {
                if (!target.HasTag(s)) yield return s;
            }

            foreach(Coding t in target)
            {
                yield return t;
            }
            
            //return ...FilterOnFhirSchemes();
        }

        public static IEnumerable<Coding> AffixTags(this Meta target, Meta source)
        {

            IEnumerable<Coding> targetTags = target.Tag ?? Enumerable.Empty<Coding>();
            IEnumerable<Coding> sourceTags = source.Tag ?? Enumerable.Empty<Coding>();
            return targetTags.AffixTags(sourceTags);
        }

        public static IEnumerable<Coding> AffixTags(this Resource target, Resource source)
        {
            if (target.Meta == null) target.Meta = new Meta();
            if (source.Meta == null) source.Meta = new Meta(); // !! side effect / mh
            return AffixTags(target.Meta, source.Meta);
        }

        public static void AffixTags(this Resource target, Parameters parameters)
        {
            if (target.Meta == null) target.Meta = new Meta();
            Meta meta = parameters.ExtractMeta().FirstOrDefault();
            if (meta != null)
            {
                target.Meta.Tag = AffixTags(target.Meta, meta).ToList();
            }
            
        }



        
    }

    public static class ModelParametersExtensions
    {
        public static IEnumerable<Meta> ExtractMeta(this Parameters parameters)
        {
            foreach(var parameter in parameters.Parameter.Where(p => p.Name == "meta"))
            {
                Meta meta = (parameter.Value as Meta);
                if (meta != null)
                {
                    yield return meta;
                }

            }
        }

        public static IEnumerable<Coding> ExtractTags(this Parameters parameters)
        {
            return parameters.ExtractMeta().SelectMany(m => m.Tag);
        }
    }
    
}