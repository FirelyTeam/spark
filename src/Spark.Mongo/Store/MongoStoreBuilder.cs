using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;
using Spark.Mongo.Search.Common;
using Spark.Mongo.Search.Indexer;
using Spark.Mongo.Store.Extensions;
using Spark.Search.Mongo;
using Spark.Store.Mongo;

namespace Spark.Mongo.Store
{
    public class MongoStoreBuilder : IStorageBuilder
    {
        private readonly string _mongoUrl;
        private readonly ILocalhost _localhost;

        public MongoStoreBuilder(string mongoUrl, ILocalhost localhost)
        {
            _mongoUrl = mongoUrl;
            _localhost = localhost;
        }

        public IFhirStore GetStore()
        {
            return new MongoFhirStore(_mongoUrl);
        }

        public IHistoryStore GetHistoryStore()
        {
          return new HistoryStore(_mongoUrl);
        }

        public IIndexStore GetIndexStore()
        {
            return new MongoIndexStore(_mongoUrl, new MongoIndexMapper());
        }

        public IFhirIndex GetFhirIndex()
        {
            MongoIndexStore indexStore = new MongoIndexStore(_mongoUrl, new MongoIndexMapper());
            return new MongoFhirIndex(indexStore, new MongoSearcher(indexStore, _localhost, new FhirModel()));
        }

        public ISnapshotStore GeSnapshotStore()
        {
            return new MongoSnapshotStore(_mongoUrl);
        }

        public T GetStore<T>()
        {
            throw new System.NotImplementedException();
        }
    }
}