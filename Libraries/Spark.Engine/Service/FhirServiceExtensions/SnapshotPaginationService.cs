/* 
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Interfaces;
using Spark.Engine.Store.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spark.Engine.Service.FhirServiceExtensions;

internal class SnapshotPaginationService : ISnapshotPagination
{
    private readonly IFhirIndex _fhirIndex;
    private readonly IFhirStore _fhirStore;
    private readonly ITransfer _transfer;
    private readonly ILocalhost _localhost;
    private readonly ISnapshotPaginationCalculator _snapshotPaginationCalculator;
    private readonly Snapshot _snapshot;
    private readonly IFhirModel _fhirModel;

    public SnapshotPaginationService(IFhirIndex fhirIndex, IFhirStore fhirStore, ITransfer transfer, ILocalhost localhost, ISnapshotPaginationCalculator snapshotPaginationCalculator, Snapshot snapshot, IFhirModel fhirModel)
    {
        _fhirIndex = fhirIndex ?? throw new ArgumentNullException(nameof(fhirIndex));
        _fhirStore = fhirStore ?? throw new ArgumentNullException(nameof(fhirStore));
        _transfer = transfer ?? throw new ArgumentNullException(nameof(transfer));
        _localhost = localhost ?? throw new ArgumentNullException(nameof(localhost));
        _snapshotPaginationCalculator = snapshotPaginationCalculator ?? throw new ArgumentNullException(nameof(snapshotPaginationCalculator));
        _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        _fhirModel = fhirModel ?? throw new ArgumentNullException(nameof(fhirModel));
    }

    public async Task<Bundle> GetPageAsync(int? index = null, Action<Entry> transformElement = null)
    {
        if (_snapshot == null)
            throw Error.NotFound("There is no paged snapshot");

        if (!_snapshot.InRange(index ?? 0))
        {
            throw Error.NotFound(
                "The specified index lies outside the range of available results ({0}) in snapshot {1}",
                _snapshot.Keys.Count(), _snapshot.Id);
        }

        return await CreateBundleAsync(index).ConfigureAwait(false);
    }

    private async Task<Bundle> CreateBundleAsync(int? offset = null)
    {
        var bundle = new Bundle
        {
            Type = _snapshot.Type,
            Total = _snapshot.Count,
            Id = Guid.NewGuid().ToString()
        };

        if (_snapshot.IsCountOnly)
        {
            bundle.SelfLink = _localhost.Absolute(new Uri(_snapshot.FeedSelfLink, UriKind.RelativeOrAbsolute));
            return bundle;
        }

        var keys = _snapshotPaginationCalculator.GetKeysForPage(_snapshot, offset).ToList();
        var entries = (await _fhirStore.GetAsync(keys, _snapshot.Elements).ConfigureAwait(false)).ToList();
        if (_snapshot.SortBy != null)
        {
            entries = entries.Select(e => new {Entry = e, Index = keys.IndexOf(e.Key)})
                .OrderBy(e => e.Index)
                .Select(e => e.Entry).ToList();
        }
        var included = await GetIncludesRecursiveForAsync(entries, _snapshot.Includes).ConfigureAwait(false);
        entries.Append(included);

        var revIncluded = await GetRevIncludeAsync(entries, _snapshot.ReverseIncludes);
        entries.Append(revIncluded);

        _transfer.Externalize(entries);
        bundle.Append(entries);

        if (offset is null or 0 && _snapshot.Outcome != null)
            bundle.AppendOutcome(_snapshot.Outcome);

        BuildLinks(bundle, offset);

        return bundle;
    }

    private async Task<List<Entry>> GetIncludesRecursiveForAsync(List<Entry> entries, IEnumerable<string> includes)
    {
        List<Entry> included = [];
        var latest = await GetIncludesForAsync(entries, includes).ConfigureAwait(false);
        int previousCount;
        do
        {
            previousCount = included.Count;
            included.AppendDistinct(latest);
            latest = await GetIncludesForAsync(latest, includes).ConfigureAwait(false);
        } while (included.Count > previousCount);
        return included;
    }

    private async Task<List<Entry>> GetIncludesForAsync(List<Entry> entries, IEnumerable<string> includes)
    {
        if (includes == null || !includes.Any())
            return [];

        var paths = includes.SelectMany(IncludeToPath);
        var identifiers = entries
            .GetResources()
            .GetReferences(paths)
            .Distinct()
            .Select(IKey (reference) => Key.ParseOperationPath(reference))
            .ToList();
        return (await _fhirStore.GetAsync(identifiers).ConfigureAwait(false)).ToList();
    }

        
    private async Task<IList<Entry>> GetRevIncludeAsync(List<Entry> entries, IEnumerable<string> revIncludes)
    {
        if (revIncludes == null || !revIncludes.Any())
            return [];

        var searchResults = await _fhirIndex.GetReverseIncludesAsync(entries.Select(e => e.Key).ToList(), revIncludes.ToList());
        if (!searchResults.Any())
            return [];

        return await _fhirStore.GetAsync(searchResults.Select(Key.ParseOperationPath)).ConfigureAwait(false);
    }

    private void BuildLinks(Bundle bundle, int? offset = null)
    {
        bundle.SelfLink = offset == null
            ? _localhost.Absolute(new Uri(_snapshot.FeedSelfLink, UriKind.RelativeOrAbsolute))
            : BuildSnapshotPageLink(offset);
        bundle.FirstLink = BuildSnapshotPageLink(0);
        bundle.LastLink = BuildSnapshotPageLink(_snapshotPaginationCalculator.GetIndexForLastPage(_snapshot));

        int? previousPageIndex = _snapshotPaginationCalculator.GetIndexForPreviousPage(_snapshot, offset);
        if (previousPageIndex != null)
        {
            bundle.PreviousLink = BuildSnapshotPageLink(previousPageIndex);
        }

        int? nextPageIndex = _snapshotPaginationCalculator.GetIndexForNextPage(_snapshot, offset);
        if (nextPageIndex != null)
        {
            bundle.NextLink = BuildSnapshotPageLink(nextPageIndex);
        }
    }

    private Uri BuildSnapshotPageLink(int? offset)
    {
        if (offset == null)
            return null;

        var baseUrl = string.IsNullOrEmpty(_snapshot.Id)
            ?
            // Stateless pagination
            new Uri(_snapshot.FeedSelfLink)
            :
            // Stateful pagination
            _localhost.Absolute(new Uri(FhirRestOp.SNAPSHOT, UriKind.Relative))
                .AddParam(FhirParameter.SNAPSHOT_ID, _snapshot.Id);

        return baseUrl.AddParam(FhirParameter.OFFSET, offset.ToString());
    }

    private IEnumerable<string> IncludeToPath(string include)
    {
        var include_ = include.Split(':');
        var resource = include_.FirstOrDefault();
        var parameterName = include_.Skip(1).FirstOrDefault();
        var searchParameter = _fhirModel.SearchParameters
            .FirstOrDefault(parameter =>
                parameter.Resource == resource && parameter.Name == parameterName
            );
        return searchParameter == null
            ? Enumerable.Empty<string>()
            : searchParameter.Path;
    }
}
