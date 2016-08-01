using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Newtonsoft.Json.Converters;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Service;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public class InteractionBuilder
    {
        private readonly ILocalhost localhost;
        private readonly IFhirIndex fhirIndex;
        private Dictionary<Bundle.HTTPVerb, Func<Bundle.EntryComponent, IEnumerable<Entry>>> builders; 
        public InteractionBuilder(ILocalhost localhost, IFhirModel fhirModel, IFhirIndex fhirIndex)
        {
            this.localhost = localhost;
            this.fhirIndex = fhirIndex;

            builders = new Dictionary<Bundle.HTTPVerb, Func<Bundle.EntryComponent, IEnumerable<Entry>>>();
            InitializeInternalBuilders();
        }

        public void RegisterBuilder(Bundle.HTTPVerb method, Func<Bundle.EntryComponent, IEnumerable<Entry>> entryBuilder)
        {
            builders.Add(method, entryBuilder);
        }

        public IEnumerable<Entry> GetEntries(Bundle bundle)
        {
            foreach (var entryComponent in bundle.Entry)
            {
                Key key = localhost.ExtractKey(entryComponent);
                Bundle.HTTPVerb method = localhost.ExtrapolateMethod(entryComponent, key);
                foreach (var entry in builders[method](entryComponent))
                {
                    yield return entry;
                }
            }
        }

        private void InitializeInternalBuilders()
        {
            builders.Add(Bundle.HTTPVerb.POST, CreatePostInteraction);
            builders.Add(Bundle.HTTPVerb.PUT, CreatePutInteraction);
            builders.Add(Bundle.HTTPVerb.DELETE, CreateDeleteInteraction);
        }

        private IEnumerable<Entry> CreatePostInteraction(Bundle.EntryComponent entryComponent)
        {
            Entry postEntry = null;
            Key key = localhost.ExtractKey(entryComponent);
            Bundle.HTTPVerb method = localhost.ExtrapolateMethod(entryComponent, key); //CCR: is key needed? Isn't method required?

            SearchParams searchCommand = GetSearchParams(entryComponent, method); 

            if (searchCommand != null && searchCommand.Parameters.Count > 0)
            {
                string localKeyValue = fhirIndex.Search(key.TypeName?? entryComponent.Resource.TypeName, searchCommand).SingleOrDefault();
                    //throw exception. probably we should manually throw this in order to add fhir specific details
                if (string.IsNullOrEmpty(localKeyValue) == false)
                {
                    Key localKey = Key.ParseOperationPath(localKeyValue);
                    postEntry = ConditionalEntry.Create(Bundle.HTTPVerb.GET, localKey, null, key);
                }
            }
            postEntry = postEntry ?? Entry.POST(key, entryComponent.Resource);

            yield return postEntry;
        }

        private IEnumerable<Entry> CreateDeleteInteraction(Bundle.EntryComponent entryComponent)
        {
            Key key = localhost.ExtractKey(entryComponent);
            Bundle.HTTPVerb method = localhost.ExtrapolateMethod(entryComponent, key);
            SearchParams parameters = GetSearchParams(entryComponent, method);

            if (parameters != null)
            {
                foreach (var localKeyValue in fhirIndex.Search(key.TypeName, parameters))
                {
                    yield return Entry.DELETE(Key.ParseOperationPath(localKeyValue), DateTimeOffset.UtcNow);
                }
            }
            else
            {
                yield return Entry.DELETE(key, DateTimeOffset.UtcNow);
            }
        }

        private IEnumerable<Entry> CreatePutInteraction(Bundle.EntryComponent entryComponent)
        {
            Key key = localhost.ExtractKey(entryComponent);
            Bundle.HTTPVerb method = localhost.ExtrapolateMethod(entryComponent, key);
            SearchParams parameters = GetSearchParams(entryComponent, method);
            Entry entry = null;

            if (parameters != null)
            {
                string localKeyValue = fhirIndex.Search(key.TypeName??entryComponent.Resource.TypeName, parameters).SingleOrDefault();
                if (localKeyValue != null)
                {
                    IKey localKey = Key.ParseOperationPath(localKeyValue);

                   entry = ConditionalEntry.Create(Bundle.HTTPVerb.PUT, localKey,
                        entryComponent.Resource, key); //if no key is found, should we do POST?
                }
                else
                {
                    entry = Entry.POST(key, entryComponent.Resource);
                }
            }

            entry = entry ?? Entry.PUT(key, entryComponent.Resource);
            yield return entry;
        }

        private SearchParams GetSearchParams(Bundle.EntryComponent entry, Bundle.HTTPVerb method)
        {
            string searchUri = null;
            UriKind kind = UriKind.RelativeOrAbsolute;
            ;
            if (method == Bundle.HTTPVerb.POST)
            {
                searchUri = string.Format("{0}?{1}", entry.TypeName, entry.Request.IfNoneExist);
                kind = UriKind.Relative;
            }
            else if (method == Bundle.HTTPVerb.DELETE || method == Bundle.HTTPVerb.PUT)
            {
                searchUri = entry.Request.Url;
            }

            return searchUri != null ? ParseQueryString(searchUri, kind) : null;
        }

        private SearchParams ParseQueryString(string path, UriKind uriKind = UriKind.RelativeOrAbsolute)
        {
            Uri absoluteUri = localhost.Absolute(new Uri(path, uriKind));
            NameValueCollection keysCollection = UriExtensions.ParseQueryString(absoluteUri);

            IEnumerable<Tuple<string, string>> searchValues =
             keysCollection.Keys.Cast<string>()
                 .Select(k => new Tuple<string, string>(k, keysCollection[k]));

            return SearchParams.FromUriParamList(searchValues);
        }
    }
}