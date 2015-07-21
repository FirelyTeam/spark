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

        public static Key ExtractKey(this ILocalhost localhost, Bundle.BundleEntryComponent entry)
        {
            if (entry.Transaction != null && entry.Transaction.Url != null)
            {
                return localhost.UriToKey(entry.Transaction.Url);
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

        private static Bundle.HTTPVerb ExtrapolateMethod(this ILocalhost localhost, Bundle.BundleEntryComponent entry, IKey key)
        {
            return entry.Transaction.Method ?? DetermineMethod(localhost, key);
        }

        public static Interaction ToInteraction(this ILocalhost localhost, Bundle.BundleEntryComponent bundleEntry)
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

        public static Bundle.BundleEntryComponent TranslateToSparseEntry(this Interaction interaction)
        {
            var entry = new Bundle.BundleEntryComponent();

            if (interaction.HasResource())
            {
                entry.Resource = interaction.Resource;
                interaction.Key.ApplyTo(entry.Resource);
            }
            return entry;
        }

        public static Bundle.BundleEntryComponent ToTransactionEntry(this Interaction interaction)
        {
            var entry = new Bundle.BundleEntryComponent();

            if (entry.Transaction == null)
            {
                entry.Transaction = new Bundle.BundleEntryTransactionComponent();
            }
            entry.Transaction.Method = interaction.Method;
            entry.Transaction.Url = interaction.Key.ToUri().ToString();

            if (interaction.HasResource())
            {
                entry.Resource = interaction.Resource;
                interaction.Key.ApplyTo(entry.Resource);
            }

            return entry;
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

        public static bool HasResource(this Bundle.BundleEntryComponent entry)
        {
            return (entry.Resource != null);
        }

        public static IEnumerable<Resource> GetResources(this Bundle bundle)
        {
            return bundle.Entry.Where(e => e.HasResource()).Select(e => e.Resource);
        }

        public static Bundle Append(this Bundle bundle, Interaction interaction)
        {
            // API: The api should have a function for this. AddResourceEntry doesn't cut it.
            // Might TransactionBuilder be better suitable?

            Bundle.BundleEntryComponent entry;
            switch (bundle.Type)
            {
                case Bundle.BundleType.History: entry = interaction.ToTransactionEntry(); break;
                case Bundle.BundleType.Searchset: entry = interaction.TranslateToSparseEntry(); break;
                default: entry = interaction.TranslateToSparseEntry(); break;
            }
            bundle.Entry.Add(entry);

            return bundle;
        }

        public static Bundle Append(this Bundle bundle, IEnumerable<Interaction> interactions)
        {
            foreach (Interaction interaction in interactions)
            {
                // BALLOT: whether to send transactionResponse components... not a very clean solution
                bundle.Append(interaction);
            }
            
            // NB! Total can not be set by counting bundle elements, because total is about the snapshot total
            // bundle.Total = bundle.Entry.Count();

            return bundle;
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
