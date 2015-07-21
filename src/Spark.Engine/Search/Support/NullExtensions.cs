/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/ewoutkramer/fhir-net-api/master/LICENSE
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

        public static bool IsNullOrEmpty(this Primitive element)
        {
            if (element == null) return true;

            if (element.ObjectValue == null) return true;

            return true;
        }
                
    }
}
