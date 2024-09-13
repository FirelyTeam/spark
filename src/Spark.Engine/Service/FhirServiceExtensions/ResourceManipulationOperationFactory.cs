/*
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2024, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;
using Spark.Engine.Extensions;

namespace Spark.Engine.Service.FhirServiceExtensions;

public static partial class ResourceManipulationOperationFactory
{
    private static readonly Dictionary<Bundle.HTTPVerb, Func<Resource, IKey, ISearchService, SearchParams, Task<ResourceManipulationOperation>>> _asyncBuilders;

    static ResourceManipulationOperationFactory()
    {
        _asyncBuilders = new Dictionary<Bundle.HTTPVerb, Func<Resource, IKey, ISearchService, SearchParams, Task<ResourceManipulationOperation>>>
        {
            { Bundle.HTTPVerb.POST, CreatePostAsync },
            { Bundle.HTTPVerb.PUT, CreatePutAsync },
            { Bundle.HTTPVerb.PATCH, CreatePatchAsync },
            { Bundle.HTTPVerb.DELETE, CreateDeleteAsync },
            { Bundle.HTTPVerb.GET, CreateGetAsync },
        };
    }
        
    private static async Task<SearchResults> GetSearchResultAsync(IKey key, ISearchService searchService, SearchParams searchParams = null)
    {
        if (searchParams == null || searchParams.Parameters.Count == 0)
            return null;
        if (searchParams != null && searchService == null)
            throw new InvalidOperationException("Unallowed operation");
        return await searchService.GetSearchResultsAsync(key.TypeName, searchParams).ConfigureAwait(false);
    }

    public static async Task<ResourceManipulationOperation> CreatePostAsync(Resource resource, IKey key, ISearchService searchService = null, SearchParams searchParams = null)
    {
        return new PostManipulationOperation(resource, key, await GetSearchResultAsync(key, searchService, searchParams).ConfigureAwait(false), searchParams);
    }

    public static async Task<ResourceManipulationOperation> CreatePutAsync(Resource resource, IKey key, ISearchService searchService = null, SearchParams searchParams = null)
    {
        return new PutManipulationOperation(resource, key, await GetSearchResultAsync(key, searchService, searchParams).ConfigureAwait(false), searchParams);
    }

    private static async Task<ResourceManipulationOperation> CreatePatchAsync(Resource resource, IKey key, ISearchService searchService = null, SearchParams searchParams = null)
    {
        return new PatchManipulationOperation(resource, key, await GetSearchResultAsync(key, searchService, searchParams), searchParams);
    }

    public static async Task<ResourceManipulationOperation> CreateDeleteAsync(IKey key, ISearchService searchService = null, SearchParams searchParams = null)
    {
        return new DeleteManipulationOperation(null, key, await GetSearchResultAsync(key, searchService, searchParams).ConfigureAwait(false), searchParams);
    }
        
    private static async Task<ResourceManipulationOperation> CreateDeleteAsync(Resource resource, IKey key, ISearchService searchService = null, SearchParams searchParams = null)
    {
        return new DeleteManipulationOperation(null, key, await GetSearchResultAsync(key, searchService, searchParams).ConfigureAwait(false), searchParams);
    }

    private static async Task<ResourceManipulationOperation> CreateGetAsync(Resource resource, IKey key, ISearchService searchService, SearchParams searchParams)
    {
        return new GetManipulationOperation(resource, key, await GetSearchResultAsync(key, searchService, searchParams), searchParams);
    }

    public static async Task<ResourceManipulationOperation> GetManipulationOperationAsync(Bundle.EntryComponent entryComponent, ILocalhost localhost, ISearchService searchService = null)
    {
        Bundle.HTTPVerb method = localhost.ExtrapolateMethod(entryComponent, null);
        Key key = localhost.ExtractKey(entryComponent);
        var searchUri = GetSearchUri(entryComponent, method);

        var searchParams = searchUri != null ? ParseQueryString(localhost, searchUri) : null;
        return await _asyncBuilders[method](entryComponent.Resource, key, searchService, searchParams)
            .ConfigureAwait(false);
    }

    private static Uri GetSearchUri(Bundle.EntryComponent entryComponent, Bundle.HTTPVerb method)
    {
        Uri searchUri = null;
        if (method == Bundle.HTTPVerb.POST)
        {
            searchUri = PostManipulationOperation.ReadSearchUri(entryComponent);
        }
        else if (method == Bundle.HTTPVerb.PUT)
        {
            searchUri = PutManipulationOperation.ReadSearchUri(entryComponent);
        }
        else if (method == Bundle.HTTPVerb.DELETE)
        {
            searchUri = DeleteManipulationOperation.ReadSearchUri(entryComponent);
        }
        else if (method == Bundle.HTTPVerb.GET)
        {
            searchUri = FhirServiceExtensions.GetManipulationOperation.ReadSearchUri(entryComponent);
        }
        return searchUri;
    }

    private static SearchParams ParseQueryString(ILocalhost localhost, Uri searchUri)
    {

        Uri absoluteUri = localhost.Absolute(searchUri);
        NameValueCollection keysCollection = UriExtensions.ParseQueryString(absoluteUri);

        IEnumerable<Tuple<string, string>> searchValues =
            keysCollection.Keys.Cast<string>()
                .Select(k => new Tuple<string, string>(k, keysCollection[k]));

        return SearchParams.FromUriParamList(searchValues);
    }
}