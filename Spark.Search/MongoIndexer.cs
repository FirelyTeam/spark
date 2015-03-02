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
using Spark.Core;

namespace Spark.Search
{

    

    
    public class MongoIndexStore
    {
        MongoCollection<BsonDocument> collection; 
        
        public MongoIndexStore(MongoCollection<BsonDocument> collection)
        {
            this.collection = collection;
        }

        public void Save(BsonDocument document)
        {
            string keyvalue = document.GetValue(InternalField.ID).ToString();
            IMongoQuery query = MongoDB.Driver.Builders.Query.EQ(InternalField.ID, keyvalue);
            
            // todo: should use Update: collection.Update();
            collection.Remove(query);
            collection.Save(document);
        }

        public void Delete(Entry entry)
        {
            string location = entry.Key.ToRelativeUri().ToString();
            string id = entry.Id.ToString();
            IMongoQuery query = MongoDB.Driver.Builders.Query.EQ(InternalField.ID, id);
            collection.Remove(query);
        }

        public void Clean()
        {
            collection.RemoveAll();
         }

    }


    public class Indexer : IIndexer
    {
        MongoIndexStore store;
        private Definitions definitions;
        private static volatile object transaction = new object();

        public Indexer(MongoIndexStore store)
        {
            this.store = store;
        }

        // IIndexer
        public void Process(Entry entry)
        {
            lock (transaction)
            {
                process(entry);
            }
        }

        public void Process(IEnumerable<Entry> entries)
        {
            lock (transaction)
            {
                foreach (Entry e in entries)
                {
                    process(e);
                }
            }
        }

        
        private void put(Entry entry, int level, DomainResource resource)
        {
            BsonIndexDocumentBuilder builder = new BsonIndexDocumentBuilder(entry);
            builder.WriteMetaData(entry, level, resource);

            var matches = definitions.MatchesFor(resource);
            foreach (Definition definition in matches)
            {
                definition.Harvest(resource, builder.InvokeCollect);
            }

            store.Save(builder.Document);
        }


        private void put(Entry entry, int level, IEnumerable<Resource> resources)
        {
            if (resources == null) return;
            foreach (var resource in resources)
            {   
                if (resource is DomainResource)
                put(entry, level, resource as DomainResource);
            }
        }

        private void put(Entry entry, int level, Resource resource)
        {
            if (resource is DomainResource)
            {
                DomainResource d = resource as DomainResource;
                put(entry, level, d);
                put(entry, level + 1, d.Contained);
            }
            
        }
        
        private void put(Entry entry)
        {
            put(entry, 0, entry.Resource);
        }

        private void process(Entry entry)
        {
            if (entry.IsResource())
            {
                put(entry, 0, entry.Resource);
            }
            else
            {
                if (entry.IsDeleted())
                {
                    store.Delete(entry);
                }
                else throw new Exception("Entry is neither resource nor deleted");
            }
            
        }

       


    }
}