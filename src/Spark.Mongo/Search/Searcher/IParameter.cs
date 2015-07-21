/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using MongoDB.Driver;
using System.Linq;

namespace Spark.Search.Mongo
{
    public enum Strain
    {
        Simple,         // field=value
        Chained,        // fieldx.fieldy.fieldz=value
        Universal,      // Valid for all resources _id, _tag, (and unofficially _limit)
        Meta,           // fields that do not filter the results (_include, _limit, _count)
        Internal,       // Index field for internal use, never user specified
        Undefined,      // Not recognized parameter
        Empty           // Paramater without value
    }

    public interface IParameter
    {
        Strain Strain { get; set; }
        string Field { get; set; }
        IMongoQuery ToQuery();
    }

    public static class IParameterLogic
    {
        public static bool IsA(this IParameter parameter, params Strain[] strain)
        {
            return strain.Contains(parameter.Strain);
        }
    }
}
