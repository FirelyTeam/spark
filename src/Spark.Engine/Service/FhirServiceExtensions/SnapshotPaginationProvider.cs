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
    public class SnapshotPaginationProvider : ISnapshotPaginationProvider
    {
        private IFhirStore _fhirStore;
        private readonly ITransfer _transfer;
        private readonly ILocalhost _localhost;
        private readonly ISnapshotPaginationCalculator _snapshotPaginationCalculator;
     
        public SnapshotPaginationProvider(IFhirStore fhirStore, ITransfer transfer, ILocalhost localhost, ISnapshotPaginationCalculator snapshotPaginationCalculator)
        {
            _fhirStore = fhirStore;
            _transfer = transfer;
            _localhost = localhost;
            _snapshotPaginationCalculator = snapshotPaginationCalculator;
        }

        public ISnapshotPagination StartPagination(Snapshot snapshot)
        {
            return new SnapshotPaginationService(_fhirStore, _transfer, _localhost, _snapshotPaginationCalculator, snapshot);
        }
    }
}