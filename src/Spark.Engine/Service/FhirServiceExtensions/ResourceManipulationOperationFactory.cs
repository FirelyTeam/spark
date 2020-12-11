using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;
using Spark.Engine.Extensions;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    using System.Threading.Tasks;

    public static partial class ResourceManipulationOperationFactory
    {
        private static readonly Dictionary<Bundle.HTTPVerb, Func<Resource, IKey, ISearchService, SearchParams, Task<ResourceManipulationOperation>>> builders;
        private static ISearchService searchService;

        static ResourceManipulationOperationFactory()
        {
            builders = new Dictionary<Bundle.HTTPVerb, Func<Resource, IKey, ISearchService, SearchParams, Task<ResourceManipulationOperation>>>();
            builders.Add(Bundle.HTTPVerb.POST, CreatePost);
            builders.Add(Bundle.HTTPVerb.PUT, CreatePut);
            builders.Add(Bundle.HTTPVerb.DELETE, CreateDelete);
        }

        public static async Task<ResourceManipulationOperation> CreatePost(Resource resource, IKey key, ISearchService service = null, SearchParams command = null)
        {
            searchService = service;
            var searchResults = await GetSearchResult(key, command).ConfigureAwait(false);
            return new PostManipulationOperation(resource, key, searchResults, command);
        }

        private static Task<SearchResults> GetSearchResult(IKey key, SearchParams command = null)
        {
            if (command == null || command.Parameters.Count == 0)
                return null;
            if (command != null && searchService == null)
                throw new InvalidOperationException("Unallowed operation");
            return searchService.GetSearchResults(key.TypeName, command);
        }

        public static async Task<ResourceManipulationOperation> CreatePut(Resource resource, IKey key, ISearchService service = null, SearchParams command = null)
        {
            searchService = service;
            var searchResults = await GetSearchResult(key, command).ConfigureAwait(false);
            return new PutManipulationOperation(resource, key, searchResults, command);
        }

        public static async Task<ResourceManipulationOperation> CreateDelete(IKey key, ISearchService service = null, SearchParams command = null)
        {
            searchService = service;
            var searchResults = await GetSearchResult(key, command).ConfigureAwait(false);
            return new DeleteManipulationOperation(null, key, searchResults, command);
        }

        private static async Task<ResourceManipulationOperation> CreateDelete(Resource resource, IKey key, ISearchService service = null, SearchParams command = null)
        {
            searchService = service;
            var searchResults = await GetSearchResult(key, command).ConfigureAwait(false);
            return new DeleteManipulationOperation(null, key, searchResults, command);
        }

        public static Task<ResourceManipulationOperation> GetManipulationOperation(Bundle.EntryComponent entryComponent, ILocalhost localhost, ISearchService service = null)
        {
            searchService = service;
            Bundle.HTTPVerb method = localhost.ExtrapolateMethod(entryComponent, null); //CCR: is key needed? Isn't method required?
            Key key = localhost.ExtractKey(entryComponent);
            var searchUri = GetSearchUri(entryComponent, method);

            return builders[method](entryComponent.Resource, key, service, searchUri != null ? ParseQueryString(localhost, searchUri) : null);
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