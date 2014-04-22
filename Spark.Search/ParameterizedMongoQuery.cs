using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Search
{
    public class ParameterizedMongoQuery: IMongoQuery
    {
        public ParameterizedMongoQuery (IMongoQuery query)
        {
            this.Query = query;
        }

        public IMongoQuery Query { get; set; }
        private SortedDictionary<String, BsonValue> Parameters = new SortedDictionary<String, BsonValue>();

        public void AddParameter(String parameterName)
        {
            AddParameter(parameterName, null);
        }

        public void AddParameter(String parameterName, BsonValue value)
        {
            Parameters.Add(parameterName, value);
        }

        public void SetParameter(String parameterName, BsonValue value)
        {
            Parameters[parameterName] = value;
        }
    }
}
