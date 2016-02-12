using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using Spark.Engine.Core;
using Spark.Core;

namespace Spark.Engine.Extensions
{

    public static class InteractionExtensions
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

        public static Interaction ToInteraction(this ILocalhost localhost, Bundle.EntryComponent bundleEntry)
        {
            Key key = localhost.ExtractKey(bundleEntry);
            Bundle.HTTPVerb method = localhost.ExtrapolateMethod(bundleEntry, key);

            if (key != null)
            {
                return Interaction.Create(method, key, bundleEntry.Resource);
            }
            else
            {
                return Interaction.Create(method, bundleEntry.Resource);
            }
            
        }

        public static Bundle.EntryComponent TranslateToSparseEntry(this Interaction interaction)
        {
            var entry = new Bundle.EntryComponent();

            ConnectResource(interaction, entry);
            return entry;
        }

        public static Bundle.EntryComponent ToTransactionEntry(this Interaction interaction)
        {
            var entry = new Bundle.EntryComponent();

            if (entry.Request == null)
            {
                entry.Request = new Bundle.RequestComponent();
            }
            entry.Request.Method = interaction.Method;
            entry.Request.Url = interaction.Key.ToUri().ToString();

            ConnectResource(interaction, entry);

            return entry;
        }

        private static void ConnectResource(Interaction interaction, Bundle.EntryComponent entry)
        {
            if (interaction.HasResource())
            {
                entry.Resource = interaction.Resource;
                interaction.Key.ApplyTo(entry.Resource);
                entry.FullUrl = interaction.Key.ToUriString();
            }
        }

        public static bool HasResource(this Interaction entry)
        {
            return (entry.Resource != null);
        }

        public static bool IsDeleted(this Interaction entry)
        {
            // API: HTTPVerb should have a broader scope than Bundle.
            return entry.Method == Bundle.HTTPVerb.DELETE;
        }

        public static bool Present(this Interaction entry)
        {
            return (entry.Method == Bundle.HTTPVerb.POST) || (entry.Method == Bundle.HTTPVerb.PUT);
        }


        public static void Append(this IList<Interaction> list, IList<Interaction> appendage)
        {
            foreach(Interaction interaction in appendage)
            {
                list.Add(interaction);
            }
        }

        public static bool Contains(this IList<Interaction> list, Interaction item)
        {
            IKey key = item.Key;
            return list.FirstOrDefault(i => i.Key.EqualTo(item.Key)) != null;
        }

        public static void AppendDistinct(this IList<Interaction> list, IList<Interaction> appendage)
        {
            foreach(Interaction item in appendage)
            {
                if (!list.Contains(item))
                {
                    list.Add(item);
                }
            }
        }

        public static IEnumerable<Resource> GetResources(this IEnumerable<Interaction> interactions)
        {
            return interactions.Where(i => i.HasResource()).Select(i => i.Resource);
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

        public static Bundle Replace(this Bundle bundle, IEnumerable<Interaction> entries)
        {
            bundle.Entry = entries.Select(e => e.TranslateToSparseEntry()).ToList();
            return bundle;
        }

        // If an interaction has no base, you should be able to supplement it (from the containing bundle for example)
        public static void SupplementBase(this Interaction interaction, string _base)
        {
            Key key = interaction.Key.Clone();
            if (!key.HasBase())
            {
                key.Base = _base;
                interaction.Key = key;
            }
        }

        public static void SupplementBase(this Interaction interaction, Uri _base)
        {
            SupplementBase(interaction, _base.ToString());
        }

        public static IEnumerable<Interaction> Transferable(this IEnumerable<Interaction> interactions)
        {
            return interactions.Where(i => i.State == InteractionState.Undefined);
        }

        public static void Assert(this InteractionState state, InteractionState correct)
        {
            if (state != correct)
            {
                throw Error.Internal("Interaction was in an invalid state");
            }
        }
    }
}
