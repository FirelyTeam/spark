/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */
using MongoDB.Driver;
using System.Collections.Generic;
using Spark.Store.Mongo;
using MongoDB.Bson;

namespace Spark.MetaStore
{
    public class MetaContext 
    {
        private readonly IMongoDatabase _db;
        private IMongoCollection<BsonDocument> _collection;

        public MetaContext(IMongoDatabase db)
        {
            _db = db;
            _collection = db.GetCollection<BsonDocument>(Collection.RESOURCE);
        }

        public List<ResourceStat> GetResourceStats()
        {
            var stats = new List<ResourceStat>();
            List<string> names = Hl7.Fhir.Model.ModelInfo.SupportedResources;

            foreach(string name in names)
            {
                FilterDefinition<BsonDocument> query = Builders<BsonDocument>.Filter
                    .And(
                        Builders<BsonDocument>.Filter.Eq(Field.TYPENAME, name),
                        Builders<BsonDocument>.Filter.Eq(Field.STATE, Value.CURRENT)
                    );
                long count = _collection.CountDocuments(query);
                stats.Add(new ResourceStat() { ResourceName = name, Count = count });
            }
            return stats;
        }
    }
}