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
        private readonly SparkSettings _sparkSettings;

        public FhirExtensionsBuilder(IStorageBuilder fhirStoreBuilder, Uri baseUri, IIndexService indexService, SparkSettings sparkSettings = null)
        {
            _fhirStoreBuilder = fhirStoreBuilder;
            _baseUri = baseUri;
            _indexService = indexService;
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
            IFhirIndex fhirStore = _fhirStoreBuilder.GetStore<IFhirIndex>();
            if (fhirStore != null)
                return new SearchService(new Localhost(_baseUri), new FhirModel(), fhirStore, _indexService);
            return null;
        }

        protected virtual IFhirServiceExtension GetHistory()
        {
            IHistoryStore historyStore = _fhirStoreBuilder.GetStore<IHistoryStore>();
            if (historyStore != null)
                return new HistoryService(historyStore);
            return null;
        }

        protected virtual IFhirServiceExtension GetCapabilityStatement()
        {
            return new CapabilityStatementService(new Localhost(_baseUri));
        }

        protected virtual IFhirServiceExtension GetPaging()
        {
            IFhirStore fhirStore = _fhirStoreBuilder.GetStore<IFhirStore>();
            ISnapshotStore snapshotStore = _fhirStoreBuilder.GetStore<ISnapshotStore>();
            IIdentityGenerator storeGenerator = _fhirStoreBuilder.GetStore<IIdentityGenerator>();
            if (fhirStore != null)
                return new PagingService(snapshotStore, new SnapshotPaginationProvider(fhirStore, new Transfer(storeGenerator, new Localhost(_baseUri), _sparkSettings), new Localhost(_baseUri), new SnapshotPaginationCalculator()));
            return null;
        }

        protected virtual IFhirServiceExtension GetStorage()
        {
            IFhirStore fhirStore = _fhirStoreBuilder.GetStore<IFhirStore>();
            IIdentityGenerator fhirGenerator = _fhirStoreBuilder.GetStore<IIdentityGenerator>();
            if (fhirStore != null)
                return new ResourceStorageService(new Transfer(fhirGenerator, new Localhost(_baseUri), _sparkSettings),  fhirStore);
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