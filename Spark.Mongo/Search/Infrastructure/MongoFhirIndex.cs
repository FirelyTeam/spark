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

namespace Spark.MongoSearch
{
    public class MongoFhirIndex : IFhirIndex
    {
        private Definitions definitions;
        private MongoSearcher searcher;
        private MongoIndexer indexer;


        public MongoFhirIndex(MongoIndexStore store, Definitions definitions)
        {
            this.definitions = definitions;
            this.indexer = new MongoIndexer(store);
            this.searcher = new MongoSearcher(store);
        }

        private object transaction = new object();
       
               
        private void process(Interaction interaction)
        {
            if (interaction.IsResource())
            {
                indexer.Put(interaction.Resource);
            }
            else if (interaction.IsDeleted())
            {
                indexer.Delete(interaction.Key);
            }
        }

        private void process(IEnumerable<Bundle.BundleEntryComponent> entries)
        {
            foreach (var entry in entries)
            {
                Process(entry);
            }
        }

        public void Process(Bundle.BundleEntryComponent entry)
        {
            lock(transaction)
            {
                process(entry);
            }
        }

        public void Delete(Interaction entry)
        {
            lock (transaction)
            {
                indexer.Delete(entry.Key);
            }
        }

        public void Process(Bundle bundle)
        {
            lock (transaction)
            {
                var updates = bundle.Entry.Where(e => e.IsResource());
                process(updates);

                var deletes = bundle.Entry.Where(e => e.IsDeleted());
                process(deletes);
            }
        }

             
        public void Clean()
        {
            lock (transaction)
            {
                indexer.Clean();
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

        public SearchResults Search(string resource, IEnumerable<Tuple<string, string>> query)
        {
            Parameters parameters = ParameterFactory.Parameters(this.definitions, resource, query);
            return searcher.Search(parameters);
        }

        public SearchResults Search(IEnumerable<Tuple<string, string>> query)
        {
            // ballot: database wide search?
            // A database wide search requires understanding of parameter types.
            // Currently this requires a ResourceType. We see no need for a database wide search.
            return Search(null, query);
        }


        public SearchResults Search(Query query)
        {
            return searcher.Search(query);
        }


        public void Process(IEnumerable<Interaction> bundle)
        {
            throw new NotImplementedException();
        }

        public void Process(Interaction interaction)
        {
            throw new NotImplementedException();
        }

        public SearchResults Search(Hl7.Fhir.Model.Parameters query)
        {
            throw new NotImplementedException();
        }
    }
}
