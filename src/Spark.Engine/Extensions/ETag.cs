/* 
 * Copyright (c) 2015-2018, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System.Net.Http.Headers;

namespace Spark.Engine.Extensions
{
    public static class ETag
    {
        public static EntityTagHeaderValue Create(string value)
        {
            string tag = "\"" + value + "\"";
            return new EntityTagHeaderValue(tag, true);
        }
    }
}
