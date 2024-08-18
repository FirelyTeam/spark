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
