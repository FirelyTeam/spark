/* 
 * Copyright (c) 2014-2018, Firely (info@fire.ly) and contributors
 * Copyright (c) 2021-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using System.Collections.Generic;
using Spark.Engine.Core;

namespace Spark.Engine.Extensions
{
    public static class BundleExtensions
    {
        public static void Append(this Bundle bundle, Resource resource)
        {
            bundle.Entry.Add(CreateEntryForResource(resource));
        }

        public static void Append(this Bundle bundle, Bundle.HTTPVerb method, Resource resource)
        {
            Bundle.EntryComponent entry = CreateEntryForResource(resource);

            if (entry.Request == null) entry.Request = new Bundle.RequestComponent();
            entry.Request.Method = method;
            bundle.Entry.Add(entry);
        }

        private static Bundle.EntryComponent CreateEntryForResource(Resource resource)
        {
            return new Bundle.EntryComponent
            {
                Resource = resource,
                FullUrl = resource.ExtractKey().ToUriString()
            };
        }

        public static void Append(this Bundle bundle, IEnumerable<Resource> resources)
        {
            foreach (Resource resource in resources)
            {
                bundle.Append(resource);
            }
        }

        public static void Append(this Bundle bundle, Bundle.HTTPVerb method, IEnumerable<Resource> resources)
        {
            foreach (Resource resource in resources)
            {
                bundle.Append(method, resource);
            }
        }

        public static Bundle Append(this Bundle bundle, Entry entry, FhirResponse response = null)
        {
            Bundle.EntryComponent bundleEntry = bundle.Type switch
            {
                Bundle.BundleType.History => entry.ToTransactionEntry(),
                Bundle.BundleType.Searchset => entry.TranslateToSparseEntry(),
                Bundle.BundleType.BatchResponse => entry.TranslateToSparseEntry(response),
                Bundle.BundleType.TransactionResponse => entry.TranslateToSparseEntry(response),
                _ => entry.TranslateToSparseEntry(),
            };

            bundle.Entry.Add(bundleEntry);

            return bundle;
        }

        public static Bundle Append(this Bundle bundle, IEnumerable<Entry> entries)
        {
            foreach (Entry entry in entries)
            {
                // BALLOT: whether to send transactionResponse components... not a very clean solution
                bundle.Append(entry);
            }

            return bundle;
        }

        public static IList<Entry> GetEntries(this ILocalhost localhost, Bundle bundle)
        {
            var entries = new List<Entry>();
            foreach(var bundleEntry in bundle.Entry)
            {
                Entry entry = localhost.ToInteraction(bundleEntry);
                entries.Add(entry);
            }

            return entries;
        }
    }
}