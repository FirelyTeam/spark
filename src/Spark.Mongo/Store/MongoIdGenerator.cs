using System;
using Hl7.Fhir.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Core;
using Spark.Store.Mongo;

namespace Spark.Mongo.Store
{
    public class MongoIdGenerator : IGenerator
    {
        IMongoDatabase database;

        public MongoIdGenerator(string mongoUrl)
        {
            this.database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
        }
        string IGenerator.NextResourceId(Resource resource)
        {
            string id = this.Next(resource.TypeName);
            return string.Format(Format.RESOURCEID, id);
        }
        
        string IGenerator.NextVersionId(string resourceIdentifier)
        {
            throw new NotImplementedException();
        }

        string IGenerator.NextVersionId(string resourceType, string resourceIdentifier)
        {
            string name = resourceType + "_history_" + resourceIdentifier;
            string versionId = this.Next(name);
            return string.Format(Format.VERSIONID, versionId);
        }

        public string Next(string name)
        {
            var collection = database.GetCollection<BsonDocument>(Collection.COUNTERS);

            var query = Builders<BsonDocument>.Filter.Eq(Field.PRIMARYKEY, name);
            var update = Builders<BsonDocument>.Update.Inc(Field.COUNTERVALUE, 1);
            var options = new FindOneAndUpdateOptions<BsonDocument>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After,
                Projection = Builders<BsonDocument>.Projection.Include(Field.COUNTERVALUE)
            };
            var document = collection.FindOneAndUpdate(query, update, options);

            string value = document[Field.COUNTERVALUE].AsInt32.ToString();
            return value;
        }
        
        public static class Format
        {
            public static string RESOURCEID = "{0}";
            public static string VERSIONID = "{0}";
        }
    }
}