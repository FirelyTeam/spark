/* 
    Lucifer: The Lucene FHIR Search, Query and Indexing Module 
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
using Spark.Data.MongoDB;

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

       
        public void Put(ResourceEntry entry)
        {
            lock (transaction)
            {
                indexer.Put(entry);
            }
        }
        public void Delete(DeletedEntry entry)
        {
            lock (transaction)
            {
                indexer.Delete(entry);
            }
        }
        public void Process(BundleEntry entry)
        {
            if (entry is ResourceEntry)
                Put(entry as ResourceEntry);
            else if (entry is DeletedEntry)
                Delete(entry as DeletedEntry);
        }
        public void Process(IEnumerable<BundleEntry> bundle)
        {
            lock (transaction)
            {
                var updates = bundle.Where(e => e is ResourceEntry).Select(e => (e as ResourceEntry));
                indexer.Put(updates);

                var deletes = bundle.Where(e => e is DeletedEntry).Select(e => (e as DeletedEntry));
                indexer.Delete(deletes);
            }
        }
        public void Process(Bundle bundle)
        {
            Process(bundle.Entries);
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
            return Search(parameters);
        }
        public SearchResults Search(string resource, IEnumerable<Tuple<string, string>> query)
        {
            Parameters parameters = ParameterFactory.Parameters(this.definitions, resource, query);
            return Search(parameters);
        }
        public SearchResults Search(IEnumerable<Tuple<string, string>> query)
        {
            // ballot: database wide search?
            // A database wide search requires understanding of parameter types.
            // Currently this requires a ResourceType. We see no need for a database wide search.
            return Search(null, query);
        }
    }
}
