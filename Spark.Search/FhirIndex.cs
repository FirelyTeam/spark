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
using Spark.Store;

namespace Spark.Search
{
    public class FhirIndex : IFhirIndex
    {
        private Definitions definitions;
        private ISearcher searcher;
        private IIndexer indexer;

        public FhirIndex(Definitions definitions, IIndexer indexer, ISearcher searcher)
        {
            this.definitions = definitions;
            this.indexer = indexer;
            this.searcher = searcher;
        }

        private object transaction = new object();
       
               
        private void process(Bundle.BundleEntryComponent entry)
        {
            if (entry.IsResource())
            {
                indexer.Put(entry.Resource);
            }
            else if (entry.IsDeleted())
            {
                Key key = entry.GetKey();
                indexer.Delete(key);
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

        public void Delete(Bundle.BundleEntryComponent entry)
        {
            lock (transaction)
            {
                Key key = entry.GetKey();
                indexer.Delete(key);
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
    }
}
