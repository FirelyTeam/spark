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
        private readonly string mongoUrl;
        private readonly ILocalhost localhost;

        public MongoStoreBuilder(string mongoUrl, ILocalhost localhost)
        {
            this.mongoUrl = mongoUrl;
            this.localhost = localhost;
        }

        public IFhirStore GetStore()
        {
            return new MongoFhirStore(mongoUrl);
        }

        public IHistoryStore GetHistoryStore()
        {
          return new HistoryStore(mongoUrl);
        }

        public IIndexStore GetIndexStore()
        {
            return new MongoIndexStore(mongoUrl, new MongoIndexMapper());
        }

        public IFhirIndex GetFhirIndex()
        {
            MongoIndexStore indexStore = new MongoIndexStore(mongoUrl, new MongoIndexMapper());
            return new MongoFhirIndex(indexStore, new MongoSearcher(indexStore, localhost, new FhirModel()));
        }

        public ISnapshotStore GeSnapshotStore()
        {
            return new MongoSnapshotStore(mongoUrl);
        }

        public T GetStore<T>()
        {
            throw new System.NotImplementedException();
        }
    }
}