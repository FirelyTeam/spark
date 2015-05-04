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
using System.Text;
using System.Threading.Tasks;

using Hl7.Fhir.Support;
using Hl7.Fhir.Model;
using System.Collections.Specialized;
using MongoDB.Driver;
using MongoDB.Bson;
using Spark.Core;
using Hl7.Fhir.Rest;

namespace Spark.Search.Mongo
{
    public class MongoFhirIndex : IFhirIndex
    {
        private Definitions definitions;
        private MongoSearcher searcher;
        private MongoIndexer indexer;
        private MongoIndexStore indexStore;


        public MongoFhirIndex(MongoIndexStore store, Definitions definitions)
        {
            this.definitions = definitions;
            this.indexStore = store;
            this.indexer = new MongoIndexer(store, definitions);
            this.searcher = new MongoSearcher(store.Collection);
        }

        private object transaction = new object();
       
               
        //private void process(Interaction interaction)
        //{
        //    indexer.Process(interaction);
        //    //if (interaction.IsResource())
        //    //{
        //    //    indexer.Put(interaction.Resource);
        //    //}
        //    //else if (interaction.IsDeleted())
        //    //{
        //    //    indexer.Delete(interaction.Key);
        //    //}
        //}

        //private void process(IEnumerable<Bundle.BundleEntryComponent> entries)
        //{
        //    foreach (var entry in entries)
        //    {
        //        Process(entry);
        //    }
        //}

        //public void Process(Bundle.BundleEntryComponent entry)
        //{
        //    lock(transaction)
        //    {
        //        process(entry);
        //    }
        //}

        //public void Delete(Interaction entry)
        //{
        //    lock (transaction)
        //    {
        //        indexer.Delete(entry.Key);
        //    }
        //}

        //public void Process(Bundle bundle)
        //{
        //    lock (transaction)
        //    {
        //        var updates = bundle.Entry.Where(e => e.IsResource());
        //        process(updates);

        //        var deletes = bundle.Entry.Where(e => e.IsDeleted());
        //        process(deletes);
        //    }
        //}


        public void Clean()
        {
            lock (transaction)
            {
                indexStore.Clean();
            }
        }

        public SearchResults Search(Parameters parameters)
        {
            return searcher.Search(parameters);
        }

        public SearchResults Search(string resource, string query = "")
        {
            Parameters parameters = ParameterFactory.Parameters(definitions, resource, query);
            return searcher.Search(parameters);
        }

        public SearchResults Search(string resource, IEnumerable<Tuple<string, string>> parameters)
        {
            UriParamList actualParameters = new UriParamList(parameters);
            var searchCommand = SearchParams.FromUriParamList(parameters);
            return Search(resource, searchCommand);
        }

        public SearchResults Search(string resource, SearchParams searchCommand)
        {
            return searcher.Search(resource, searchCommand);
        }

        /* TODO: Probably delete, old implemententation based on Parameters instead of Criterium.
        public SearchResults Search(string resource, IEnumerable<Tuple<string, string>> query)
        {
            Parameters parameters = ParameterFactory.Parameters(this.definitions, resource, query);
            return searcher.Search(parameters);
        }
        */
        public SearchResults Search(IEnumerable<Tuple<string, string>> query)
        {
            // ballot: database wide search?
            // A database wide search requires understanding of parameter types.
            // Currently this requires a ResourceType. We see no need for a database wide search.
            return Search(null, query);
        }

        /*TODO: Delete, Query is obsolete.
        public SearchResults Search(Query query)
        {
            return searcher.Search(query);
        }
        */

        public void Process(IEnumerable<Interaction> interactions)
        {
            foreach (var i in interactions)
            {
                Process(i);
            }
        }

        public void Process(Interaction interaction)
        {
            indexer.Process(interaction);
        }

        //public SearchResults Search(Hl7.Fhir.Model.Parameters query)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
