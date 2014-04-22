using MongoDB.Driver;
using M = MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace Spark.Search
{
    internal class MongoQueryChain: IMongoQuery
    {
        private List<IMongoQuery> chain = new List<IMongoQuery>();

        internal MongoQueryChain(IMongoQuery startQuery)
        {
            chain.Add(startQuery);
        }

        internal IMongoQuery Pop()
        {
            var result = chain.Last();
            chain.Remove(result);
            return result;
        }

        internal void Push(IMongoQuery query)
        {
            chain.Add(query);
        }

        internal IMongoQuery FillInAndPop(IEnumerable<BsonDocument> keys)
        {
            return Pop().SetParameter("keys", new BsonArray(keys));
        }

        public override string ToString()
        {
            return chain.First().ToString();
        }
    }
}
