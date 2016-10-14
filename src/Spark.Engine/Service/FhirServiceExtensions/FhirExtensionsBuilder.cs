using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;
using Spark.Service;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public class FhirExtensionsBuilder : IFhirExtensionsBuilder
    {
        private readonly IStorageBuilder fhirStoreBuilder;
        private readonly Uri baseUri;
        private readonly IList<IFhirServiceExtension> extensions;

        public FhirExtensionsBuilder(IStorageBuilder fhirStoreBuilder, Uri baseUri)
        {
            this.fhirStoreBuilder = fhirStoreBuilder;
            this.baseUri = baseUri;
            var extensionBuilders = new Func<IFhirServiceExtension>[]
           {
                GetSearch,
                GetHistory,
                GetConformance,
                GetPaging,
                GetStorage
           };
            extensions = extensionBuilders.Select(builder => builder()).Where(ext => ext != null).ToList();
        }

        protected virtual IFhirServiceExtension GetSearch()
        {
            IFhirIndex fhirStore = fhirStoreBuilder.GetStore<IFhirIndex>();
            if (fhirStore!= null)
                return new SearchService(new Localhost(baseUri),  new FhirModel(), fhirStore);
            return null;
        }

        protected virtual IFhirServiceExtension GetHistory()
        {
            IHistoryStore historyStore = fhirStoreBuilder.GetStore<IHistoryStore>();
            if (historyStore != null)
                return new HistoryService(historyStore);
            return null;
        }

        protected virtual IFhirServiceExtension GetConformance()
        {
            return new ConformanceService(new Localhost(baseUri));
        }


        protected virtual IFhirServiceExtension GetPaging()
        {
            IFhirStore fhirStore = fhirStoreBuilder.GetStore<IFhirStore>();
            ISnapshotStore snapshotStore = fhirStoreBuilder.GetStore<ISnapshotStore>();
            IGenerator storeGenerator = fhirStoreBuilder.GetStore<IGenerator>();
            if (fhirStore != null)
                return new PagingService(snapshotStore, new SnapshotPaginationProvider(fhirStore, new Transfer(storeGenerator, new Localhost(baseUri)), new Localhost(baseUri), new SnapshotPaginationCalculator()));
            return null;
        }

        protected virtual IFhirServiceExtension GetStorage()
        {
            IFhirStore fhirStore = fhirStoreBuilder.GetStore<IFhirStore>();
            IGenerator fhirGenerator = fhirStoreBuilder.GetStore<IGenerator>();
            if (fhirStore != null)
                return new ResourceStorageService(new Transfer(fhirGenerator, new Localhost(baseUri)),  fhirStore);
            return null;
        }

        public IEnumerable<IFhirServiceExtension> GetExtensions()
        {
            return extensions;
        }

        public IEnumerator<IFhirServiceExtension> GetEnumerator()
        {
            return extensions.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}