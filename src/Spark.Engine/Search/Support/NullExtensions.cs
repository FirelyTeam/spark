/* 
 * Copyright (c) 2015-2018, Furore (info@furore.com) and contributors
 * Copyright (c) 2020-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System.Collections;
using Hl7.Fhir.Model;

namespace Spark.Search.Support
{
    public static class NullExtensions
    {
        public static bool IsNullOrEmpty(this IList list)
        {
            if (list == null) return true;

            return list.Count == 0;
        }

        public static bool IsNullOrEmpty(this PrimitiveType element)
        {
            if (element == null) return true;

            if (element.ObjectValue == null) return true;

            return true;
        }
    }
}
