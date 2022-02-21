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
        private readonly IStorageBuilder _fhirStoreBuilder;
        private readonly Uri _baseUri;
        private readonly IList<IFhirServiceExtension> _extensions;
        private readonly IIndexService _indexService;
        private readonly IAsyncIndexService _asyncIndexService;
        private readonly SparkSettings _sparkSettings;

        public FhirExtensionsBuilder(
            IStorageBuilder fhirStoreBuilder, 
            Uri baseUri, 
            IIndexService indexService, 
            IAsyncIndexService asyncIndexService,
            SparkSettings sparkSettings = null)
        {
            _fhirStoreBuilder = fhirStoreBuilder;
            _baseUri = baseUri;
            _indexService = indexService;
            _asyncIndexService = asyncIndexService;
            _sparkSettings = sparkSettings;
            var extensionBuilders = new Func<IFhirServiceExtension>[]
           {
                GetSearch,
                GetHistory,
                GetCapabilityStatement,
                GetPaging,
                GetStorage
           };
            _extensions = extensionBuilders.Select(builder => builder()).Where(ext => ext != null).ToList();
        }

        protected virtual IFhirServiceExtension GetSearch()
        {
            var fhirIndex = _fhirStoreBuilder.GetStore<IFhirIndex>();
            var asyncFhirIndex = _fhirStoreBuilder.GetStore<IAsyncFhirIndex>();
            if (fhirIndex != null && asyncFhirIndex != null)
                return new SearchService(new Localhost(_baseUri), 
                    new FhirModel(), 
                    fhirIndex, 
                    asyncFhirIndex, 
                    _indexService, 
                    _asyncIndexService);
            return null;
        }

        protected virtual IFhirServiceExtension GetHistory()
        {
            var historyStore = _fhirStoreBuilder.GetStore<IHistoryStore>();
            var asyncHistoryStore = _fhirStoreBuilder.GetStore<IAsyncHistoryStore>();
            if (historyStore != null && asyncHistoryStore != null)
                return new HistoryService(historyStore, asyncHistoryStore);
            return null;
        }

        protected virtual IFhirServiceExtension GetCapabilityStatement()
        {
            return new CapabilityStatementService(new Localhost(_baseUri));
        }

        protected virtual IFhirServiceExtension GetPaging()
        {
            var fhirStore = _fhirStoreBuilder.GetStore<IFhirStore>();
            var asyncFhirStore = _fhirStoreBuilder.GetStore<IAsyncFhirStore>();
            var snapshotStore = _fhirStoreBuilder.GetStore<ISnapshotStore>();
            var asyncSnapshotStore = _fhirStoreBuilder.GetStore<IAsyncSnapshotStore>();
            var storeGenerator = _fhirStoreBuilder.GetStore<IGenerator>();
            if (fhirStore != null && asyncFhirStore != null)
            {
                var snapshotPaginationProvider = new SnapshotPaginationProvider(fhirStore, asyncFhirStore,
                    new Transfer(storeGenerator, new Localhost(_baseUri), _sparkSettings),
                    new Localhost(_baseUri), new SnapshotPaginationCalculator());
                return new PagingService(snapshotStore, asyncSnapshotStore,
                    snapshotPaginationProvider, snapshotPaginationProvider);
            }

            return null;
        }

        protected virtual IFhirServiceExtension GetStorage()
        {
            var fhirStore = _fhirStoreBuilder.GetStore<IFhirStore>();
            var asyncFhirStore = _fhirStoreBuilder.GetStore<IAsyncFhirStore>();
            var fhirGenerator = _fhirStoreBuilder.GetStore<IGenerator>();
            if (fhirStore != null)
                return new ResourceStorageService(new Transfer(fhirGenerator, new Localhost(_baseUri), _sparkSettings),
                    fhirStore, asyncFhirStore);
            return null;
        }

        public IEnumerable<IFhirServiceExtension> GetExtensions()
        {
            return _extensions;
        }

        public IEnumerator<IFhirServiceExtension> GetEnumerator()
        {
            return _extensions.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
