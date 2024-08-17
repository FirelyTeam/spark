/* 
 * Copyright (c) 2016-2018, Furore (info@furore.com) and contributors
 * Copyright (c) 2020-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using System;
using Hl7.Fhir.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Core;
using Spark.Store.Mongo;

namespace Spark.Mongo.Store
{
    public class MongoIdGenerator : IIdentityGenerator
    {
        public static string RESOURCEID = "{0}";
        public static string VERSIONID = "{0}";

        private readonly IMongoDatabase _database;

        public MongoIdGenerator(string mongoUrl)
        {
            _database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
        }
        string IIdentityGenerator.NextResourceId(Resource resource)
        {
            string id = Next(resource.TypeName);
            return string.Format(RESOURCEID, id);
        }
        
        string IIdentityGenerator.NextVersionId(string resourceIdentifier)
        {
            throw new NotImplementedException();
        }

        string IIdentityGenerator.NextVersionId(string resourceType, string resourceIdentifier)
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