using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using Spark.Engine.Core;
using Spark.Core;

namespace Spark.Engine.Extensions
{

    public static class EntryExtensions
    {

        public static Key ExtractKey(this ILocalhost localhost, Bundle.EntryComponent entry)
        {
            if (entry.Request != null && entry.Request.Url != null)
            {
                return localhost.UriToKey(entry.Request.Url);
            }
            else if (entry.Resource != null)
            {
                return entry.Resource.ExtractKey();
            }
            else
            {
                return null;
            }
        }

        private static Bundle.HTTPVerb DetermineMethod(ILocalhost localhost, IKey key)
        {
            if (key == null) return Bundle.HTTPVerb.DELETE; // probably...

            switch (localhost.GetKeyKind(key))
            {
                case KeyKind.Foreign: return Bundle.HTTPVerb.POST;
                case KeyKind.Temporary: return Bundle.HTTPVerb.POST;
                case KeyKind.Internal: return Bundle.HTTPVerb.PUT;
                case KeyKind.Local: return Bundle.HTTPVerb.PUT;
                default: return Bundle.HTTPVerb.PUT;
            }
        }

        private static Bundle.HTTPVerb ExtrapolateMethod(this ILocalhost localhost, Bundle.EntryComponent entry, IKey key)
        {
            return entry.Request.Method ?? DetermineMethod(localhost, key);
        }

        public static Entry ToInteraction(this ILocalhost localhost, Bundle.EntryComponent bundleEntry)
        {
            Key key = localhost.ExtractKey(bundleEntry);
            Bundle.HTTPVerb method = localhost.ExtrapolateMethod(bundleEntry, key);

            if (key != null)
            {
                return Entry.Create(method, key, bundleEntry.Resource);
            }
            else
            {
                return Entry.Create(method, bundleEntry.Resource);
            }
            
        }

        public static Bundle.EntryComponent TranslateToSparseEntry(this Entry entry)
        {
            var bundleEntry = new Bundle.EntryComponent();

            SetBundleEntryResource(entry, bundleEntry);
            return bundleEntry;
        }

        public static Bundle.EntryComponent ToTransactionEntry(this Entry entry)
        {
            var bundleEntry = new Bundle.EntryComponent();

            if (bundleEntry.Request == null)
            {
                bundleEntry.Request = new Bundle.RequestComponent();
            }
            bundleEntry.Request.Method = entry.Method;
            bundleEntry.Request.Url = entry.Key.ToUri().ToString();

            SetBundleEntryResource(entry, bundleEntry);

            return bundleEntry;
        }

        private static void SetBundleEntryResource(Entry entry, Bundle.EntryComponent bundleEntry)
        {
            if (entry.HasResource())
            {
                bundleEntry.Resource = entry.Resource;
                entry.Key.ApplyTo(bundleEntry.Resource);
                bundleEntry.FullUrl = entry.Key.ToUriString();
            }
        }

        public static bool HasResource(this Entry entry)
        {
            return (entry.Resource != null);
        }

        public static bool IsDeleted(this Entry entry)
        {
            // API: HTTPVerb should have a broader scope than Bundle.
            return entry.Method == Bundle.HTTPVerb.DELETE;
        }

        public static bool Present(this Entry entry)
        {
            return (entry.Method == Bundle.HTTPVerb.POST) || (entry.Method == Bundle.HTTPVerb.PUT);
        }


        public static void Append(this IList<Entry> list, IList<Entry> appendage)
        {
            foreach(Entry entry in appendage)
            {
                list.Add(entry);
            }
        }

        public static bool Contains(this IList<Entry> list, Entry item)
        {
            IKey key = item.Key;
            return list.FirstOrDefault(i => i.Key.EqualTo(item.Key)) != null;
        }

        public static void AppendDistinct(this IList<Entry> list, IList<Entry> appendage)
        {
            foreach(Entry item in appendage)
            {
                if (!list.Contains(item))
                {
                    list.Add(item);
                }
            }
        }

        public static IEnumerable<Resource> GetResources(this IEnumerable<Entry> entries)
        {
            return entries.Where(i => i.HasResource()).Select(i => i.Resource);
        }

        private static bool isValidResourcePath(string path, Resource resource)
        {
            string name = path.Split('.').FirstOrDefault();
            return resource.TypeName == name;
        }

        public static IEnumerable<string> GetReferences(this Resource resource, string path)
        {
            if (!isValidResourcePath(path, resource)) return Enumerable.Empty<string>();

            ElementQuery query = new ElementQuery(path);
            var list = new List<string>();

            query.Visit(resource, element =>
                {
                    if (element is ResourceReference)
                    {
                        string reference = (element as ResourceReference).Reference;
                        if (reference != null)
                        {
                            list.Add(reference);
                        }
                    }
                });
            return list;
        }

        public static IEnumerable<string> GetReferences(this IEnumerable<Resource> resources, string path)
        {
            return resources.SelectMany(r => r.GetReferences(path));
            //foreach (Resource entry in resources)
            //{
            //    IEnumerable<string> list = GetLocalReferences(entry, include);
            //    foreach (Uri value in list)
            //    {
            //        if (value != null)
            //            yield return value;
            //    }
            //}
        }

        public static IEnumerable<string> GetReferences(this IEnumerable<Resource> resources, IEnumerable<string> paths)
        {
            return paths.SelectMany(i => resources.GetReferences(i));
        }


        // BALLOT: bundle now basically has two versions. One for history (with transaction elements) and a regular one (without transaction elements) This is so ugly and so NOT FHIR

        // BALLOT: The identifying elements of a resource are too spread out over the bundle
        // It should be in the same location. Either on resource.meta or entry.meta or entry.transaction

        // BALLOT: transaction/transactionResponse in bundle is named wrongly. Because the bundle is the transaction. Not the entry.
        // better use http/rest terminology: request / response.

        /*
            bundle
	            - base
	            - total
	            - entry *
		            - request 
		            - response
		            - resource
			            - meta
				            - id
				            - versionid
        */

        public static Bundle Replace(this Bundle bundle, IEnumerable<Entry> entries)
        {
            bundle.Entry = entries.Select(e => e.TranslateToSparseEntry()).ToList();
            return bundle;
        }

        // If an interaction has no base, you should be able to supplement it (from the containing bundle for example)
        public static void SupplementBase(this Entry entry, string _base)
        {
            Key key = entry.Key.Clone();
            if (!key.HasBase())
            {
                key.Base = _base;
                entry.Key = key;
            }
        }

        public static void SupplementBase(this Entry entry, Uri _base)
        {
            SupplementBase(entry, _base.ToString());
        }

        public static IEnumerable<Entry> Transferable(this IEnumerable<Entry> entries)
        {
            return entries.Where(i => i.State == EntryState.Undefined);
        }

        public static void Assert(this EntryState state, EntryState correct)
        {
            if (state != correct)
            {
                throw Error.Internal("Interaction was in an invalid state");
            }
        }
    }
}
