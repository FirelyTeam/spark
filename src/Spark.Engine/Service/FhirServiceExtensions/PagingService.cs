using System;
using System.Threading.Tasks;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public class PagingService : IPagingService
    {
        private readonly ISnapshotStore _snapshotstore;
        private readonly ISnapshotPaginationProvider _paginationProvider;

        public PagingService(ISnapshotStore snapshotstore, ISnapshotPaginationProvider paginationProvider)
        {
            _snapshotstore = snapshotstore;
            _paginationProvider = paginationProvider;
        }

        public ISnapshotPagination StartPagination(Snapshot snapshot)
        {
            return Task.Run(() => StartPaginationAsync(snapshot)).GetAwaiter().GetResult();
        }

        public ISnapshotPagination StartPagination(string snapshotkey)
        {
            return Task.Run(() => StartPaginationAsync(snapshotkey)).GetAwaiter().GetResult();
        }

        public async Task<ISnapshotPagination> StartPaginationAsync(Snapshot snapshot)
        {
            if (_snapshotstore != null)
            {
                await _snapshotstore.AddSnapshotAsync(snapshot).ConfigureAwait(false);
            }
            else
            {
                snapshot.Id = null;
            }

            return _paginationProvider.StartPagination(snapshot);
        }
        public async Task<ISnapshotPagination> StartPaginationAsync(string snapshotkey)
        {
            if (_snapshotstore == null)
            {
                throw new NotSupportedException("Stateful pagination is not currently supported.");
            }
            return _paginationProvider.StartPagination(await _snapshotstore.GetSnapshotAsync(snapshotkey).ConfigureAwait(false));
        }
    }
}