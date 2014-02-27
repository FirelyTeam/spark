using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Spark.Search
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