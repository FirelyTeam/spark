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
        
        public static Interaction TranslateToInteraction(this Bundle.BundleEntryComponent bundleEntry)
        {
            IKey key = bundleEntry.ExtractKey();
            Bundle.HTTPVerb method = bundleEntry.Transaction.Method ?? Bundle.HTTPVerb.PUT; // TODO: is this the correct failback for null? 
   
            return new Interaction(key, method, DateTimeOffset.UtcNow, bundleEntry.Resource);
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
