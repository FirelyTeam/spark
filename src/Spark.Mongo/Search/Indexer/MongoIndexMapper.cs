using MongoDB.Bson;
using Spark.Engine.Model;
using Spark.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Mongo.Search.Indexer
{
    //Maps IndexValue elements to BsonElements.
    public class MongoIndexMapper
    {
        public BsonValue Map(Expression expression)
        {
            Type expressionType = expression.GetType();
            if (expressionType == typeof(IndexValue))
            {

            }
            MethodInfo m = this.GetType().GetMethod("MapExpression", new Type[] { expressionType });
            if (m != null)
            {
                return (BsonValue)m.Invoke(this, new object[] { expression});
            }

            throw new NotImplementedException("Not expected to map an abstract Expression.");
        }

        public BsonValue MapExpression(IndexValue indexValue)
        {
            return new BsonDocument(IndexValueToElement(indexValue));
        }

        private BsonElement IndexValueToElement(IndexValue indexValue)
        {
            if (indexValue.Values.Count == 1)
            {
                return new BsonElement(indexValue.Name, Map(indexValue.Values[0]));
            }
            BsonArray values = new BsonArray();
            foreach (var value in indexValue.Values)
            {
                values.Add(Map(value));
            }
            return new BsonElement(indexValue.Name, values);
        }

        public BsonValue MapExpression(CompositeValue composite)
        {
            BsonDocument compositeDocument = new BsonDocument();
            foreach (var component in composite.Components)
            {
                if (component is IndexValue)
                    compositeDocument.Add(IndexValueToElement((IndexValue)component));
                else
                    throw new ArgumentException("All Components of composite are expected to be of type IndexValue");
            }
            return compositeDocument;
        }

        public BsonValue MapExpression(StringValue stringValue)
        {
            return BsonValue.Create(stringValue.Value);
        }

        public BsonValue MapExpression(DateTimeValue datetimeValue)
        {
            return BsonValue.Create(datetimeValue.Value);
        }
    }
}
