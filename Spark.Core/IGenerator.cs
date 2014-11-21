/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core
{

    public interface IGenerator
    {
        string NextKey(string name);
    }

    public static class GeneratorExtensions
    {
        public static string NextKey(this IGenerator generator, Resource resource)
        {
            string name = resource.GetType().Name;
            return generator.NextKey(name);
        }

        public static string NextHistoryKey(this IGenerator generator, Resource resource)
        {
            string name = resource.GetType().Name + "_history";
            return generator.NextKey(name);
        }

        public static string NextHistoryKey(this IGenerator generator, string name)
        {
            name = name + "_history";
            return generator.NextKey(name);
        }
    }

}
