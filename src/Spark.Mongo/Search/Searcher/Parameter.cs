/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System.Collections.Generic;
using System.Linq;

using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Spark.Mongo.Search.Common;

namespace Spark.Search.Mongo
{
    public enum Logic { And, Or }

    public class Parameter : IParameter, ITerm
    {
        public Strain Strain {get; set; }
        public IMongoQuery ToQuery()
        {
            return Argument.BuildQuery(this);
        }

        public string Resource { get; set; }
        public string Field { get; set;} 
        public string Operator {get; set; }
        public string Value {get; set; }
        public Argument Argument { get; set; }
        public override string ToString()
        {
            return string.Format("{0}={1}", Argument.FieldToString(this), Argument.ValueToString(this));
        }
    }

    public class CompositeParameter : IParameter
    {
        public string Field { get; set; }
        public List<Parameter> parameters = new List<Parameter>();
        public Logic Logic;
        public Strain Strain { get; set; }
        public IMongoQuery ToQuery()
        {
            List<IMongoQuery> queries = new List<IMongoQuery>();
            foreach(Parameter p in parameters)
            {
                IMongoQuery q = p.ToQuery();
                queries.Add(q);
            }
            if (Logic == Logic.And)
                return Query.And(queries);
            else
                return Query.Or(queries);
        }
        public override string ToString()
        {
            string field = null;
            string separator = (Logic == Logic.And) ? "+" : ",";
            List<string> values = new List<string>();
            
            foreach (Parameter p in parameters)
            {
                if (field == null)
                {
                    field = p.Argument.FieldToString(p);
                }
                values.Add(p.Argument.ValueToString(p));
            }
            return field+"="+string.Join(separator, values); 
        }
    }

    public class ChainedParameter : IParameter
    {
        public Strain Strain { get; set; }
        public string Field {get; set; }
        public List<Join> Joins = new List<Join>();
        public IParameter Parameter; // the last item of the chain.
        public IMongoQuery ToQuery()
        {
            return null; // wordt nu opgelost in MongoSearcher (omdat er een collection voor nodig is)
        }

        public override string ToString()
        {
            string key, value;
            key = Joins.Select(j => j.ToString()).Aggregate((a, b) => a + "." + b);
            value = Joins.Last().Value;
            return string.Format("{0}:{1}", key, value);
        }
    }

    public class IncludeParameter : IParameter
    {
        public Strain Strain { get; set; }
        public string Field {get; set; }
        
        public string TargetResource { get; set; }
        public string TargetField { get; set; }

        public IMongoQuery ToQuery()
        {
            return null; // wordt nu opgelost in MongoSearcher (omdat er een collection voor nodig is)
        }
        public override string ToString()
        {
            return MetaField.INCLUDE + "=" + TargetResource + "." + TargetField;
        }
    }

    public class Join : ITerm
    {
        public string Resource { get; set; }
        public string Field { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
        public Argument Argument { get; set; }
        public override string ToString()
        {
            string s = "";
            if (Resource != null) s += "(" + Resource + ")";
            s += Field;
            if (Operator != null) s += ":" + Operator;
            return s;
        }
    }

   
}