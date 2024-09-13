/* 
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Store.Interfaces;
using Spark.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spark.Engine.Service.FhirServiceExtensions;

internal class SnapshotPaginationService : ISnapshotPagination
{
    private IFhirIndex _fhirIndex;
    private IFhirStore _fhirStore;
    private readonly ITransfer _transfer;
    private readonly ILocalhost _localhost;
    private readonly ISnapshotPaginationCalculator _snapshotPaginationCalculator;
    private readonly Snapshot _snapshot;

    public SnapshotPaginationService(IFhirIndex fhirIndex, IFhirStore fhirStore, ITransfer transfer, ILocalhost localhost, ISnapshotPaginationCalculator snapshotPaginationCalculator, Snapshot snapshot)
    {
        _fhirIndex = fhirIndex;
        _fhirStore = fhirStore;
        _transfer = transfer;
        _localhost = localhost;
        _snapshotPaginationCalculator = snapshotPaginationCalculator;
        _snapshot = snapshot;
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

    private async Task<Bundle> CreateBundleAsync(int? start = null)
    {
        Bundle bundle = new Bundle
        {
            Type = _snapshot.Type,
            Total = _snapshot.Count,
            Id = Guid.NewGuid().ToString()
        };

        List<IKey> keys = _snapshotPaginationCalculator.GetKeysForPage(_snapshot, start).ToList();
        var entries = (await _fhirStore.GetAsync(keys, _snapshot.Elements).ConfigureAwait(false)).ToList();
        if (_snapshot.SortBy != null)
        {
            entries = entries.Select(e => new {Entry = e, Index = keys.IndexOf(e.Key)})
                .OrderBy(e => e.Index)
                .Select(e => e.Entry).ToList();
        }
        IList<Entry> included = await GetIncludesRecursiveForAsync(entries, _snapshot.Includes).ConfigureAwait(false);
        entries.Append(included);

        IList<Entry> revIncluded = await GetRevIncludeAsync(entries, _snapshot.ReverseIncludes);
        entries.Append(revIncluded);

        _transfer.Externalize(entries);
        bundle.Append(entries);
        BuildLinks(bundle, start);

        return bundle;
    }
    private async Task<IList<Entry>> GetIncludesRecursiveForAsync(IList<Entry> entries, IEnumerable<string> includes)
    {
        IList<Entry> included = new List<Entry>();

        var latest = await GetIncludesForAsync(entries, includes).ConfigureAwait(false);
        int previouscount;
        do
        {
            previouscount = included.Count;
            included.AppendDistinct(latest);
            latest = await GetIncludesForAsync(latest, includes).ConfigureAwait(false);
        }
        while (included.Count > previouscount);
        return included;
    }

    private async Task<IList<Entry>> GetIncludesForAsync(IList<Entry> entries, IEnumerable<string> includes)
    {
        if (includes == null || !includes.Any()) return new List<Entry>();

        IEnumerable<string> paths = includes.SelectMany(i => IncludeToPath(i));
        IList<IKey> identifiers = entries.GetResources().GetReferences(paths).Distinct().Select(k => (IKey)Key.ParseOperationPath(k)).ToList();

        IList<Entry> result = (await _fhirStore.GetAsync(identifiers).ConfigureAwait(false)).ToList();

        return result;
    }

        
    private async Task<IList<Entry>> GetRevIncludeAsync(IList<Entry> entries, IEnumerable<string> revIncludes)
    {
        if (revIncludes == null || !revIncludes.Any()) return new List<Entry>();

        var searchResults = await _fhirIndex.GetReverseIncludesAsync(entries.Select(e => e.Key).ToList(), revIncludes.ToList());
        if (!searchResults.Any())
        {
            return new List<Entry>();
        }

        return await _fhirStore.GetAsync(searchResults.Select(k => Key.ParseOperationPath(k))).ConfigureAwait(false);
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
        string[] _include = include.Split(':');
        string resource = _include.FirstOrDefault();
        string paramname = _include.Skip(1).FirstOrDefault();
        var param = ModelInfo.SearchParameters.FirstOrDefault(p => p.Resource == resource && p.Name == paramname);
        if (param != null)
        {
            return param.Path;
        }
        else
        {
            return Enumerable.Empty<string>();
        }
    }
}