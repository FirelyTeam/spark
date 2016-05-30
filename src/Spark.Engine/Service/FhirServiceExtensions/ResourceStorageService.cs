using System.Collections.Generic;
using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Store.Interfaces;
using Spark.Service;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public class ResourceStorageService : IResourceStorageService
    {
        private readonly ITransfer transfer;
        private IFhirStore fhirStore;


        public ResourceStorageService(ITransfer transfer)
        {
            this.transfer = transfer;
        }

        public bool EnableForStore(IStorageBuilder builder)
        {
            fhirStore = builder.GetStore();
            return fhirStore != null;
        }

        public Entry Get(IKey key)
        {
            return fhirStore.Get(key);
        }

        public Entry Add(Entry entry)
        {
            transfer.Internalize(entry);
            fhirStore.Add(entry);

            Entry result = fhirStore.Get(entry.Key);
            transfer.Externalize(result);

            return result;
        }

        public IList<Entry> Get(IEnumerable<string> localIdentifiers, string sortby = null)
        {
            IList<Entry> results = fhirStore.Get(localIdentifiers, null);
            transfer.Externalize(results);
            return results;

        }
    }
}