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
using Hl7.Fhir.Rest;

namespace Spark.Engine.Extensions
{

    public static class TagHelper
    {
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
            Meta meta = parameters.ExtractMetaResources().FirstOrDefault();
            if (meta != null)
            {
                target.Meta.Tag = AffixTags(target.Meta, meta).ToList();
            }
        }
    }
}
