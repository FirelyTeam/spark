/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using System.Diagnostics;
using MongoDB.Driver;
using MongoDB.Bson;

namespace Spark.Search
{

    public class MongoIndexer : IIndexer
    {
        private Definitions definitions;
        MongoCollection<BsonDocument> collection; 
        public MongoIndexer(MongoCollection<BsonDocument> collection, Definitions definitions)
        {
            this.collection = collection;
            this.definitions = definitions;
        }

        private void Put(ResourceEntry entry, int level, Resource resource)
        {
            Document document = new Document(collection, definitions);
            document.Put(entry, level, resource);
            Put(entry, level + 1, resource.Contained);
        }
        private void Put(ResourceEntry entry, int level, IEnumerable<Resource> resources)
        {
            if (resources == null) return;
            foreach (var resource in resources)
            {
                Put(entry, level, resource);
            }
        }
        public void Put(ResourceEntry entry)
        {
            if (entry is ResourceEntry)
            {
                Put(entry, 0, entry.Resource);
            }
        }
        public void Put(IEnumerable<ResourceEntry> entries)
        {
            foreach (ResourceEntry entry in entries)
            {
                if(entry is ResourceEntry<Condition>)
                    Put(entry);
                else
                    Put(entry);
            }
        }
        public void Delete(DeletedEntry entry)
        {
            string id = entry.Id.ToString();
            IMongoQuery query = MongoDB.Driver.Builders.Query.EQ(InternalField.ID, id);
            collection.Remove(query);
        }
        public void Delete(IEnumerable<DeletedEntry> entries)
        {
            foreach (var entry in entries)
            {
                Delete(entry);
            }
        }
        public void Clean()
        {
            collection.RemoveAll();
        }
    }
}