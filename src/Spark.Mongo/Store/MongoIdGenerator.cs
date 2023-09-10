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
        public static string RESOURCEID = "{0}";
        public static string VERSIONID = "{0}";

        private readonly IMongoDatabase _database;

        public MongoIdGenerator(string mongoUrl)
        {
            _database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
        }

        private void UpdateResourceId(string typeName, int resourceId)
        {
            var collection = _database.GetCollection<BsonDocument>(Collection.COUNTERS);
            var query = Builders<BsonDocument>.Filter.Eq(Field.PRIMARYKEY, typeName);
            var update = Builders<BsonDocument>.Update
                .Set(Field.PRIMARYKEY, typeName)
                .Max(Field.COUNTERVALUE, resourceId);
            var options = new FindOneAndUpdateOptions<BsonDocument> { IsUpsert = true };
            collection.FindOneAndUpdate(query, update, options);
        }

        string IGenerator.NextResourceId(Resource resource)
        {
            if (!string.IsNullOrWhiteSpace(resource.Id) && !resource.Id.StartsWith("0") && int.TryParse(resource.Id, out var resourceId))
            {
                UpdateResourceId(resource.TypeName, resourceId);
                return string.Format(RESOURCEID, resourceId);
            }

            string id = Next(resource.TypeName);
            return string.Format(RESOURCEID, id);
        }
        
        string IGenerator.NextVersionId(string resourceIdentifier)
        {
            throw new NotImplementedException();
        }

        string IGenerator.NextVersionId(string resourceType, string resourceIdentifier)
        {
            string name = resourceType + "_history_" + resourceIdentifier;
            string versionId = Next(name);
            return string.Format(VERSIONID, versionId);
        }

        public string Next(string name)
        {
            var collection = _database.GetCollection<BsonDocument>(Collection.COUNTERS);

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
    }
}