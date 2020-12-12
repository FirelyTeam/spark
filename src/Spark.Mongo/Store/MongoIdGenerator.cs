using System;
using Hl7.Fhir.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Core;
using Spark.Store.Mongo;

namespace Spark.Mongo.Store
{
    using System.Threading.Tasks;

    public class MongoIdGenerator : IGenerator
    {
        private readonly IMongoDatabase _database;

        public MongoIdGenerator(string mongoUrl)
        {
            this._database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
        }
        async Task<string> IGenerator.NextResourceId(Resource resource)
        {
            string id = await Next(resource.TypeName).ConfigureAwait(false);
            return string.Format(Format.RESOURCEID, id);
        }

        Task<string> IGenerator.NextVersionId(string resourceIdentifier)
        {
            throw new NotImplementedException();
        }

        async Task<string> IGenerator.NextVersionId(string resourceType, string resourceIdentifier)
        {
            string name = resourceType + "_history_" + resourceIdentifier;
            string versionId = await Next(name).ConfigureAwait(false);
            return string.Format(Format.VERSIONID, versionId);
        }

        private async Task<string> Next(string name)
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
            var document = await collection.FindOneAndUpdateAsync(query, update, options).ConfigureAwait(false);

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