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
using Task = System.Threading.Tasks.Task;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public static partial class ResourceManipulationOperationFactory
    {
        private static readonly Dictionary<Bundle.HTTPVerb, Func<Resource, IKey, ISearchService, SearchParams, ResourceManipulationOperation>> _builders;
        private static readonly Dictionary<Bundle.HTTPVerb, Func<Resource, IKey, ISearchService, SearchParams, Task<ResourceManipulationOperation>>> _asyncBuilders;
        private static ISearchService _searchService;

        static ResourceManipulationOperationFactory()
        {
            _builders = new Dictionary<Bundle.HTTPVerb, Func<Resource, IKey, ISearchService, SearchParams, ResourceManipulationOperation>>
            {
                { Bundle.HTTPVerb.POST, CreatePost },
                { Bundle.HTTPVerb.PUT, CreatePut },
                { Bundle.HTTPVerb.DELETE, CreateDelete }
            };
            
            _asyncBuilders = new Dictionary<Bundle.HTTPVerb, Func<Resource, IKey, ISearchService, SearchParams, Task<ResourceManipulationOperation>>>
            {
                { Bundle.HTTPVerb.POST, CreatePostAsync },
                { Bundle.HTTPVerb.PUT, CreatePutAsync },
                { Bundle.HTTPVerb.DELETE, CreateDeleteAsync }
            };
        }

        private static SearchResults GetSearchResult(IKey key, SearchParams command = null)
        {
            if (command == null || command.Parameters.Count == 0)
                return null;
            if (command != null && _searchService == null)
                throw new InvalidOperationException("Unallowed operation");
            return _searchService.GetSearchResults(key.TypeName, command);
        }
        
        private static async Task<SearchResults> GetSearchResultAsync(IKey key, SearchParams command = null)
        {
            if (command == null || command.Parameters.Count == 0)
                return null;
            if (command != null && _searchService == null)
                throw new InvalidOperationException("Unallowed operation");
            return await _searchService.GetSearchResultsAsync(key.TypeName, command).ConfigureAwait(false);
        }

        public static ResourceManipulationOperation CreatePost(Resource resource, IKey key, ISearchService service = null, SearchParams command = null)
        {
            _searchService = service;
            return new PostManipulationOperation(resource, key, GetSearchResult(key, command), command);
        }

        public static async Task<ResourceManipulationOperation> CreatePostAsync(Resource resource, IKey key, ISearchService service = null, SearchParams command = null)
        {
            _searchService = service;
            return new PostManipulationOperation(resource, key, await GetSearchResultAsync(key, command).ConfigureAwait(false), command);
        }

        public static ResourceManipulationOperation CreatePut(Resource resource, IKey key, ISearchService service = null, SearchParams command = null)
        {
            _searchService = service;
            return new PutManipulationOperation(resource, key,GetSearchResult(key, command), command);
        }

        public static async Task<ResourceManipulationOperation> CreatePutAsync(Resource resource, IKey key, ISearchService service = null, SearchParams command = null)
        {
            _searchService = service;
            return new PutManipulationOperation(resource, key, await GetSearchResultAsync(key, command).ConfigureAwait(false), command);
        }

        public static ResourceManipulationOperation CreateDelete(IKey key, ISearchService service = null, SearchParams command = null)
        {
            _searchService = service;
            return new DeleteManipulationOperation(null, key, GetSearchResult(key, command), command);
        }

        public static async Task<ResourceManipulationOperation> CreateDeleteAsync(IKey key, ISearchService service = null, SearchParams command = null)
        {
            _searchService = service;
            return new DeleteManipulationOperation(null, key, await GetSearchResultAsync(key, command).ConfigureAwait(false), command);
        }

        private static ResourceManipulationOperation CreateDelete(Resource resource, IKey key, ISearchService service = null, SearchParams command = null)
        {
            _searchService = service;
            return new DeleteManipulationOperation(null, key, GetSearchResult(key, command), command);
        }
        
        private static async Task<ResourceManipulationOperation> CreateDeleteAsync(Resource resource, IKey key, ISearchService service = null, SearchParams command = null)
        {
            _searchService = service;
            return new DeleteManipulationOperation(null, key, await GetSearchResultAsync(key, command).ConfigureAwait(false), command);
        }

        public static ResourceManipulationOperation GetManipulationOperation(Bundle.EntryComponent entryComponent, ILocalhost localhost, ISearchService service = null)
        {
            _searchService = service;
            Bundle.HTTPVerb method = localhost.ExtrapolateMethod(entryComponent, null);
            Key key = localhost.ExtractKey(entryComponent);
            var searchUri = GetSearchUri(entryComponent, method);

            var searchParams = searchUri != null ? ParseQueryString(localhost, searchUri) : null;
            return _builders[method](entryComponent.Resource, key, service, searchParams);
        }

        public static async Task<ResourceManipulationOperation> GetManipulationOperationAsync(Bundle.EntryComponent entryComponent, ILocalhost localhost, ISearchService service = null)
        {
            _searchService = service;
            Bundle.HTTPVerb method = localhost.ExtrapolateMethod(entryComponent, null);
            Key key = localhost.ExtractKey(entryComponent);
            var searchUri = GetSearchUri(entryComponent, method);

            var searchParams = searchUri != null ? ParseQueryString(localhost, searchUri) : null;
            return await _asyncBuilders[method](entryComponent.Resource, key, service, searchParams)
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
}