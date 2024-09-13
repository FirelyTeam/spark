/* 
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;
using Spark.Service;

namespace Spark.Engine.Service.FhirServiceExtensions;

public class SnapshotPaginationProvider : ISnapshotPaginationProvider
{
    private IFhirIndex _fhirIndex;
    private IFhirStore _fhirStore;
    private readonly ITransfer _transfer;
    private readonly ILocalhost _localhost;
    private readonly ISnapshotPaginationCalculator _snapshotPaginationCalculator;
     
    public SnapshotPaginationProvider(IFhirIndex fhirIndex, IFhirStore fhirStore, ITransfer transfer, ILocalhost localhost, ISnapshotPaginationCalculator snapshotPaginationCalculator)
    {
        _fhirIndex = fhirIndex;
        _fhirStore = fhirStore;
        _transfer = transfer;
        _localhost = localhost;
        _snapshotPaginationCalculator = snapshotPaginationCalculator;
    }

    public ISnapshotPagination StartPagination(Snapshot snapshot)
    {
        return new SnapshotPaginationService(_fhirIndex, _fhirStore, _transfer, _localhost, _snapshotPaginationCalculator, snapshot);
    }
}