/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Collections.Generic;

namespace Spark.Search.Mongo
{
    public static class QueryExtentions
    {
        public static IMongoQuery BuildQuery(this Parameter parameter)
        {
            return parameter.Argument.BuildQuery(parameter);
        }

        public static IMongoQuery BuildQuery(this CompositeParameter parameter)
        {
            List<IMongoQuery> queries = new List<IMongoQuery>();
            foreach (Parameter p in parameter.parameters)
            {
                IMongoQuery q = p.ToQuery();
                queries.Add(q);
            }
            if (parameter.Logic == Logic.And)
                return Query.And(queries);
            else
                return Query.Or(queries);
        }
    }
}