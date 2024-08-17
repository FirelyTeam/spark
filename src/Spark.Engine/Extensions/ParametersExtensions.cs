/*
 * Copyright (c) 2023-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Engine.Extensions
{
    public static class ParametersExtensions
    {
        public static IEnumerable<Meta> ExtractMetaResources(this Parameters parameters)
        {
            foreach(var parameter in parameters.Parameter.Where(p => p.Name == "meta"))
            {
                if (parameter.Value is Meta meta)
                    yield return meta;
            }
        }
    }
}
