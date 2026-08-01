/*
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Engine.Core;
using Spark.Engine.Interfaces;
using Spark.Engine.Search;
using Spark.Engine.Store.Interfaces;
using System;

namespace Spark.Engine.Service.FhirServiceExtensions;

public class SnapshotPaginationProvider : ISnapshotPaginationProvider
{
    private readonly IFhirIndex _fhirIndex;
    private readonly IFhirStore _fhirStore;
    private readonly ITransfer _transfer;
    private readonly ILocalhost _localhost;
    private readonly ISnapshotPaginationCalculator _snapshotPaginationCalculator;
    private readonly IFhirModel _fhirModel;
    private readonly ResourceResolver _resourceResolver;

    public SnapshotPaginationProvider(IFhirIndex fhirIndex, IFhirStore fhirStore, ITransfer transfer,
        ILocalhost localhost, ISnapshotPaginationCalculator snapshotPaginationCalculator, IFhirModel fhirModel,
        ResourceResolver resourceResolver)
    {
        _fhirIndex = fhirIndex ?? throw new ArgumentNullException(nameof(fhirIndex));
        _fhirStore = fhirStore ?? throw new ArgumentNullException(nameof(fhirStore));
        _transfer = transfer ?? throw new ArgumentNullException(nameof(transfer));
        _localhost = localhost ?? throw new ArgumentNullException(nameof(localhost));
        _snapshotPaginationCalculator = snapshotPaginationCalculator ??
                                        throw new ArgumentNullException(nameof(snapshotPaginationCalculator));
        _fhirModel = fhirModel ?? throw new ArgumentNullException(nameof(fhirModel));
        _resourceResolver = resourceResolver ?? throw new ArgumentNullException(nameof(resourceResolver));
    }

    [Obsolete(
        message: $"Use {nameof(SnapshotPaginationProvider)}(IFhirIndex, IFhirStore, ITransfer, ILocalhost, ISnapshotPaginationCalculator, IFhirModel, ResourceResolver) instead.")]
    public SnapshotPaginationProvider(IFhirIndex fhirIndex, IFhirStore fhirStore, ITransfer transfer,
        ILocalhost localhost, ISnapshotPaginationCalculator snapshotPaginationCalculator, IFhirModel fhirModel)
    {
        _fhirIndex = fhirIndex;
        _fhirStore = fhirStore;
        _transfer = transfer;
        _localhost = localhost;
        _snapshotPaginationCalculator = snapshotPaginationCalculator;
        _fhirModel = fhirModel;
    }

    public ISnapshotPagination StartPagination(Snapshot snapshot)
    {
        return new SnapshotPaginationService(
            _fhirIndex,
            _fhirStore,
            _transfer,
            _localhost,
            _snapshotPaginationCalculator,
            snapshot,
            _fhirModel,
            _resourceResolver
        );
    }
}
