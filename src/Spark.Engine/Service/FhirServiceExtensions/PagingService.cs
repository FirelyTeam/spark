using System;
using System.Threading.Tasks;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public class PagingService : IPagingService, IAsyncPagingService
    {
        private readonly ISnapshotStore _snapshotStore;
        private readonly IAsyncSnapshotStore _asyncSnapshotStore;
        private readonly ISnapshotPaginationProvider _paginationProvider;
        private readonly IAsyncSnapshotPaginationProvider _asyncSnapshotPaginationProvider;

        public PagingService(
            ISnapshotStore snapshotStore,
            IAsyncSnapshotStore asyncSnapshotStore,
            ISnapshotPaginationProvider paginationProvider,
            IAsyncSnapshotPaginationProvider asyncSnapshotPaginationProvider)
        {
            _snapshotStore = snapshotStore;
            _asyncSnapshotStore = asyncSnapshotStore;
            _paginationProvider = paginationProvider;
            _asyncSnapshotPaginationProvider = asyncSnapshotPaginationProvider;
        }

        public ISnapshotPagination StartPagination(Snapshot snapshot)
        {
            if (_snapshotStore != null)
            {
                _snapshotStore.AddSnapshot(snapshot);
            }
            else
            {
                snapshot.Id = null;
            }

            return _paginationProvider.StartPagination(snapshot);
        }

        public ISnapshotPagination StartPagination(string snapshotkey)
        {
            if (_snapshotStore == null)
            {
                throw new NotSupportedException("Stateful pagination is not currently supported.");
            }

            return _paginationProvider.StartPagination(_snapshotStore
                .GetSnapshot(snapshotkey));
        }

        public async Task<IAsyncSnapshotPagination> StartPaginationAsync(Snapshot snapshot)
        {
            if (_asyncSnapshotStore != null)
            {
                await _asyncSnapshotStore.AddSnapshotAsync(snapshot).ConfigureAwait(false);
            }
            else
            {
                snapshot.Id = null;
            }

            return _asyncSnapshotPaginationProvider.StartAsyncPagination(snapshot);
        }

        public async Task<IAsyncSnapshotPagination> StartPaginationAsync(string snapshotkey)
        {
            if (_asyncSnapshotStore == null)
            {
                throw new NotSupportedException("Stateful pagination is not currently supported.");
            }

            return _asyncSnapshotPaginationProvider.StartAsyncPagination(await _asyncSnapshotStore
                .GetSnapshotAsync(snapshotkey).ConfigureAwait(false));
        }
    }
}
