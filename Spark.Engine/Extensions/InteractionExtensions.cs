using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core
{
    public static class InteractionExtensions
    {
        
        public static Key ExtractKey(this ILocalhost localhost, Bundle.BundleEntryTransactionComponent transaction)
        {
            Uri uri = new Uri(transaction.Url, UriKind.RelativeOrAbsolute);
            return localhost.UriToKey(uri);
        }

        private static Bundle.HTTPVerb DetermineMethod(ILocalhost localhost, IKey key)
        {
            // BALLOT: this is too much a sometimes/maybe/unsure/whenever kind of logic. 
            switch (localhost.GetKeyKind(key))
            {
                case KeyKind.Foreign: return Bundle.HTTPVerb.POST;
                case KeyKind.Temporary: return Bundle.HTTPVerb.POST;
                case KeyKind.Internal: return Bundle.HTTPVerb.PUT;
                case KeyKind.Local: return Bundle.HTTPVerb.PUT;
                default: return Bundle.HTTPVerb.PUT;
            }
        }

        public static Interaction ToInteraction(this ILocalhost localhost, Bundle.BundleEntryComponent bundleEntry)
        {
            if (bundleEntry.Transaction != null)
            {
                Key key = localhost.ExtractKey(bundleEntry.Transaction);
                Bundle.HTTPVerb method = bundleEntry.Transaction.Method ?? DetermineMethod(localhost, key);

                return new Interaction(key, method, DateTimeOffset.UtcNow, bundleEntry.Resource);
            }
            else
            {
                return new Interaction(Bundle.HTTPVerb.PUT, bundleEntry.Resource);
            }
            
        }
        
        public static Bundle.BundleEntryComponent TranslateToBundleEntry(this Interaction interaction)
        {
            var bundleEntry = new Bundle.BundleEntryComponent();
            
            if (bundleEntry.Transaction == null)
                bundleEntry.Transaction = new Bundle.BundleEntryTransactionComponent();

            bundleEntry.Transaction.Url = interaction.Key.ToUri().ToString();
            bundleEntry.Transaction.Method = interaction.Method;
            bundleEntry.Resource = interaction.Resource;
            return bundleEntry;
        }

        public static bool IsResource(this Interaction entry)
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

        public static bool IsDeleted(this Bundle.BundleEntryComponent entry)
        {
            return (entry.Transaction.Method == Bundle.HTTPVerb.POST);
        }

        public static IEnumerable<Resource> GetResources(this Bundle bundle)
        {
            return bundle.Entry.Where(e => e.HasResource()).Select(e => e.Resource);
        }

        public static Bundle Append(this Bundle bundle, IEnumerable<Interaction> entries)
        {
            foreach (Interaction entry in entries)
            {
                var bundleEntry = entry.TranslateToBundleEntry();
                bundle.Entry.Add(bundleEntry);
            }
            bundle.Total = bundle.Entry.Count();

            return bundle;
        }

        public static Bundle Replace(this Bundle bundle, IEnumerable<Interaction> entries)
        {
            bundle.Entry = entries.Select(e => e.TranslateToBundleEntry()).ToList();
            return bundle;
        }

    }
}
