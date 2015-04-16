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

    public class MongoIndexer 
    {
        MongoIndexStore store;
        private Definitions definitions;

        public MongoIndexer(MongoIndexStore store)
        {
            this.store = store;
        }

        public void Process(Interaction interaction)
        {
            if (interaction.IsResource())
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
        
        private void put(Key key, int level, DomainResource resource)
        {
            BsonIndexDocumentBuilder builder = new BsonIndexDocumentBuilder(key);
            builder.WriteMetaData(key, level, resource);

            var matches = definitions.MatchesFor(resource);
            foreach (Definition definition in matches)
            {
                definition.Harvest(resource, builder.InvokeCollect);
            }

            store.Save(builder.Document);
        }

        private void put(Key key, int level, IEnumerable<Resource> resources)
        {
            if (resources == null) return;
            foreach (var resource in resources)
            {   
                if (resource is DomainResource)
                put(key, level, resource as DomainResource);
            }
        }

        private void put(Key key, int level, Resource resource)
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