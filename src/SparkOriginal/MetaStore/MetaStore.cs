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

namespace Spark.MetaStore
{
    public class MetaContext 
    {
        private MongoDatabase db;
        private MongoCollection collection;

        public MetaContext(MongoDatabase db)
        {
            this.db = db;
            collection = db.GetCollection(Collection.RESOURCE);
        }

        public List<ResourceStat> GetResourceStats()
        {
            var stats = new List<ResourceStat>();
            List<string> names = Hl7.Fhir.Model.ModelInfo.SupportedResources;

            foreach(string name in names)
            {
                IMongoQuery query = Query.And(Query.EQ(Field.TYPENAME, name), Query.EQ(Field.STATE, Value.CURRENT));
                long count = collection.Count(query);
                stats.Add(new ResourceStat() { ResourceName = name, Count = count });
            }
            return stats;
        }
    }

   

   

    

}