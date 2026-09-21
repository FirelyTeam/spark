/* 
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * Copyright (c) 2019-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Interfaces;

namespace Spark.Engine.Service.FhirServiceExtensions;

public class SearchService : ISearchService
{
    private readonly IFhirModel _fhirModel;
    private readonly ILocalhost _localhost;
    private IFhirIndex _fhirIndex;

    public SearchService(ILocalhost localhost, IFhirModel fhirModel, IFhirIndex fhirIndex)
    {
        _fhirModel = fhirModel;
        _localhost = localhost;
        _fhirIndex = fhirIndex;
    }

    public async Task<Snapshot> GetSnapshotAsync(string type, SearchParams searchCommand)
    {
        Validate.TypeName(type, _fhirModel.SupportedResources);

        UriBuilder builder = new(_localhost.Uri(type));
        Uri selflink = builder.Uri;

        if (searchCommand.Count == 0 || searchCommand.Summary == SummaryType.Count)
        {
            long count = await _fhirIndex.CountAsync(type, searchCommand).ConfigureAwait(false);
            return Snapshot.CreateCountOnly(Bundle.BundleType.Searchset, selflink, count);
        }

        SearchResults results = await _fhirIndex.SearchAsync(type, searchCommand).ConfigureAwait(false);

        if (results.HasErrors)
        {
            throw new SparkException(HttpStatusCode.BadRequest, results.Outcome);
        }

        builder.Query = results.UsedParameters;
        selflink = builder.Uri;

        return CreateSnapshot(type, selflink, results, searchCommand, results.Outcome);
    }

    public async Task<Snapshot> GetSnapshotForEverythingAsync(IKey key)
    {
        var searchCommand = new SearchParams();
        if (string.IsNullOrEmpty(key.ResourceId) == false)
        {
            searchCommand.Add("_id", key.ResourceId);
        }
        var compartment = _fhirModel.FindCompartmentInfo(key.TypeName);
        if (compartment != null)
        {
            foreach (var ri in compartment.ReverseIncludes)
            {
                searchCommand.RevInclude.Add((ri, IncludeModifier.None));
            }
        }

        return await GetSnapshotAsync(key.TypeName, searchCommand).ConfigureAwait(false);
    }

    public async Task<IKey> FindSingleAsync(string type, SearchParams searchCommand)
    {
        return Key.ParseOperationPath((await GetSearchResultsAsync(type, searchCommand).ConfigureAwait(false)).Single());
    }

    public async Task<IKey> FindSingleOrDefaultAsync(string type, SearchParams searchCommand)
    {
        string value = (await GetSearchResultsAsync(type, searchCommand).ConfigureAwait(false)).SingleOrDefault();
        return value != null ? Key.ParseOperationPath(value) : null;
    }

    public async Task<SearchResults> GetSearchResultsAsync(string type, SearchParams searchCommand)
    {
        Validate.TypeName(type, _fhirModel.SupportedResources);
        SearchResults results = await _fhirIndex.SearchAsync(type, searchCommand).ConfigureAwait(false);

        return results.HasErrors ? throw new SparkException(HttpStatusCode.BadRequest, results.Outcome) : results;
    }

    private Snapshot CreateSnapshot(
        string type,
        Uri selflink,
        IEnumerable<string> keys,
        SearchParams searchCommand,
        OperationOutcome outcome = null)
    {
        selflink = AddSearchParamsToLink(selflink, searchCommand);

        return Snapshot.Create(
            Bundle.BundleType.Searchset,
            selflink,
            keys.ToList(),
            GetFirstSort(searchCommand),
            searchCommand.Count,
            searchCommand.Include.Select(include => include.Item1).ToList(),
            searchCommand.RevInclude.Select(revInclude => revInclude.Item1).ToList(),
            GetSubsetElements(type, searchCommand),
            outcome
        );
    }

    /// <summary>The self link with the parameters that shape the result, so that paging can repeat the search.</summary>
    private static Uri AddSearchParamsToLink(Uri selflink, SearchParams searchCommand)
    {
        if (searchCommand.Count.HasValue)
        {
            //TODO: should we change count?
            //count = Math.Min(searchCommand.Count.Value, MAX_PAGE_SIZE);
            selflink = selflink.AddParam(SearchParams.SEARCH_PARAM_COUNT, searchCommand.Count.ToString());
        }

        if (searchCommand.Sort.Any())
        {
            foreach (var tuple in searchCommand.Sort)
            {
                selflink = selflink.AddParam(SearchParams.SEARCH_PARAM_SORT,
                    string.Format("{0}:{1}", tuple.Item1, tuple.Item2 == SortOrder.Ascending ? "asc" : "desc"));
            }
        }

        if (searchCommand.Include.Any())
        {
            selflink = selflink.AddParam(SearchParams.SEARCH_PARAM_INCLUDE, searchCommand.Include.Select(inc => inc.Item1).ToArray());
        }

        if (searchCommand.RevInclude.Any())
        {
            selflink = selflink.AddParam(SearchParams.SEARCH_PARAM_REVINCLUDE, searchCommand.RevInclude.Select(inc => inc.Item1).ToArray());
        }

        return selflink;
    }

    /// <summary>The elements a response is cut down to: the ones asked for, plus the ones the type must keep.</summary>
    private IReadOnlyList<string> GetSubsetElements(string type, SearchParams searchCommand)
    {
        if (searchCommand.Elements == null || searchCommand.Elements.Count == 0)
            return searchCommand.Elements?.ToList();

        List<string> elements = [.. searchCommand.Elements];
        foreach (string element in GetMandatoryAndModifierElements(type))
        {
            if (!elements.Contains(element))
                elements.Add(element);
        }

        return elements;
    }

    /// <summary>The mandatory elements of the type, and the modifier ones that change what it means.</summary>
    private IEnumerable<string> GetMandatoryAndModifierElements(string type)
    {
        ClassMapping classMapping = _fhirModel.GetModelInspector().FindClassMapping(type);

        return classMapping == null
            ? []
            : classMapping.PropertyMappings
                .Where(property => property.IsModifier || property.IsMandatoryElement)
                .Select(property => property.Name);
    }

    private static string GetFirstSort(SearchParams searchCommand)
    {
        string firstSort = null;
        if (searchCommand.Sort != null && searchCommand.Sort.Any())
        {
            firstSort = searchCommand.Sort[0].Item1; //TODO: Support sortorder and multiple sort arguments.
        }
        return firstSort;
    }
}
