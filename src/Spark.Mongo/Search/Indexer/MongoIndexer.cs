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
using Spark.Mongo.Search.Indexer;

namespace Spark.Mongo.Search.Common
{

    public class MongoIndexer 
    {
        MongoIndexStore store;
        private Definitions definitions;

        public MongoIndexer(MongoIndexStore store, Definitions definitions)
        {
            this.store = store;
            this.definitions = definitions;
        }

        public void Process(Interaction interaction)
        {
            if (interaction.HasResource())
            {
                put(interaction);
            }
            else
            {
                if (interaction.IsDeleted())
                {
                    store.Delete(interaction);
                }
                else throw new Exception("Entry is neither resource nor deleted");
            }
        }

        public void Process(IEnumerable<Interaction> interactions)
        {
            foreach (Interaction interaction in interactions)
            {
                Process(interaction);
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
        
        private void put(Interaction interaction)
        {
            put(interaction.Key, 0, interaction.Resource);
        }

    }
}