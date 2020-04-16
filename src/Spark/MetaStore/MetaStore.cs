/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Collections.Generic;
using Spark.Store.Mongo;
using MongoDB.Bson;
using System.Linq;

namespace Spark.MetaStore
{
    public class MetaContext 
    {
        private IMongoDatabase db;
        private IMongoCollection<BsonDocument> collection;

        public MetaContext(IMongoDatabase db)
        {
            this.db = db;
            collection = db.GetCollection<BsonDocument>(Collection.RESOURCE);
        }

        public List<ResourceStat> GetResourceStats()
        {
            var stats = new List<ResourceStat>();
            List<string> names = Hl7.Fhir.Model.ModelInfo.SupportedResources;

            var list = collection.Aggregate()
                .Match(Builders<BsonDocument>.Filter.Eq(Field.STATE, Value.CURRENT))
                .Group(new BsonDocument {
                    { "_id", Field.TYPENAME }, { "count", new BsonDocument("$sum",1) }
                }).ToList();
            foreach (var item in list)
            {
                var name = item["_id"].AsString;
                var count = item["count"].AsInt32;
                stats.Add(new ResourceStat() { ResourceName = name, Count = count });
            }

            foreach(string name in names)
            {
                if(stats.FirstOrDefault(s=>s.ResourceName == name).ResourceName == null)
                {
                    stats.Add(new ResourceStat() { ResourceName = name, Count = 0 });
                }
            }
            return stats;
        }
    }

   

   

    

}