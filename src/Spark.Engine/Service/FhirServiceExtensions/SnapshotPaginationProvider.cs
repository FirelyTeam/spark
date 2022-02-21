/* 
 * Copyright (c) 2016, Furore (info@furore.com) and contributors
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;
using Spark.Service;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public class SnapshotPaginationProvider : ISnapshotPaginationProvider, IAsyncSnapshotPaginationProvider
    {
        private readonly IFhirStore _fhirStore;
        private readonly IAsyncFhirStore _asyncFhirStore;
        private readonly ITransfer _transfer;
        private readonly ILocalhost _localhost;
        private readonly ISnapshotPaginationCalculator _snapshotPaginationCalculator;

        public SnapshotPaginationProvider(IFhirStore fhirStore, IAsyncFhirStore asyncFhirStore, ITransfer transfer,
            ILocalhost localhost, ISnapshotPaginationCalculator snapshotPaginationCalculator)
        {
            _fhirStore = fhirStore;
            _asyncFhirStore = asyncFhirStore;
            _transfer = transfer;
            _localhost = localhost;
            _snapshotPaginationCalculator = snapshotPaginationCalculator;
            _fhirStore = fhirStore;
        }

        public ISnapshotPagination StartPagination(Snapshot snapshot)
        {
            return new SnapshotPaginationService(_fhirStore, _asyncFhirStore, _transfer, _localhost,
                _snapshotPaginationCalculator, snapshot);
        }

        public IAsyncSnapshotPagination StartAsyncPagination(Snapshot snapshot)
        {
            return new SnapshotPaginationService(_fhirStore, _asyncFhirStore, _transfer, _localhost,
                _snapshotPaginationCalculator, snapshot);
        }
    }
}
