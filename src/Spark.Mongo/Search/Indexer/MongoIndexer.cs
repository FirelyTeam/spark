/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;

using Hl7.Fhir.Model;
using MongoDB.Bson;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Store.Interfaces;
using Spark.Mongo.Search.Indexer;

namespace Spark.Mongo.Search.Common
{

    public class MongoIndexer 
    {
        private MongoIndexStore store;
        private Definitions definitions;

        public MongoIndexer(IIndexStore store, Definitions definitions)
        {
            this.store = (MongoIndexStore)store;
            this.definitions = definitions;
        }

        public void Process(Entry entry)
        {
            if (entry.HasResource())
            {
                put(entry);
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

        public void Process(IEnumerable<Entry> entries)
        {
            foreach (Entry entry in entries)
            {
                Process(entry);
            }
        }
        
        private void put(IKey key, int level, DomainResource resource)
        {
            BsonIndexDocumentBuilder builder = new BsonIndexDocumentBuilder(key);
            builder.WriteMetaData(key, level, resource);

            var matches = definitions.MatchesFor(resource);
            foreach (Definition definition in matches)
            {
                definition.Harvest(resource, builder.InvokeWrite);
            }

            BsonDocument document = builder.ToDocument();
    
            store.Save(document);
        }

        private void put(IKey key, int level, IEnumerable<Resource> resources)
        {
            if (resources == null) return;
            foreach (var resource in resources)
            {   
                if (resource is DomainResource)
                put(key, level, resource as DomainResource);
            }
        }

        private void put(IKey key, int level, Resource resource)
        {
            if (resource is DomainResource)
            {
                DomainResource d = resource as DomainResource;
                put(key, level, d);
                put(key, level + 1, d.Contained);
            }
            
        }
        
        private void put(Entry entry)
        {
            put(entry.Key, 0, entry.Resource);
        }

    }
}