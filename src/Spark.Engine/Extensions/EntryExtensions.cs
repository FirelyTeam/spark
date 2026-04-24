/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2020-2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Spark.Engine.Core;

namespace Spark.Engine.Extensions;

internal static class EntryExtensions
{
    internal static Bundle.EntryComponent TranslateToSparseEntry(this Entry entry, FhirResponse response = null)
    {
        var bundleEntry = new Bundle.EntryComponent();
        if (response != null)
        {
            bundleEntry.Response = new Bundle.ResponseComponent
            {
                Status = $"{(int)response.StatusCode} {response.StatusCode}",
                Location = response.Key?.ToString(),
                Etag = response.Key != null ? ETag.Create(response.Key.VersionId).ToString() : null,
                LastModified =
                    entry is { Resource.Meta: not null }
                        ? entry.Resource.Meta.LastUpdated
                        : null
            };
        }

        entry.SetBundleEntryResource(bundleEntry);
        return bundleEntry;
    }

    internal static Bundle.EntryComponent ToTransactionEntry(this Entry entry)
    {
        var bundleEntry = new Bundle.EntryComponent();

        bundleEntry.Request ??= new Bundle.RequestComponent();
        bundleEntry.Request.Method = entry.Method;
        bundleEntry.Request.Url = entry.Key.ToUri().ToString();

        entry.SetBundleEntryResource(bundleEntry);

        return bundleEntry;
    }

    private static void SetBundleEntryResource(this Entry entry, Bundle.EntryComponent bundleEntry)
    {
        if (!entry.HasResource())
            return;
        bundleEntry.Resource = entry.Resource;
        entry.Key.ApplyTo(bundleEntry.Resource);
        bundleEntry.FullUrl = entry.Key.ToUriString();
    }

    internal static bool HasResource(this Entry entry)
    {
        return entry.Resource != null;
    }

    internal static bool IsDeleted(this Entry entry)
    {
        // API: HTTPVerb should have a broader scope than Bundle.
        return entry.Method == Bundle.HTTPVerb.DELETE;
    }

    internal static void Append(this IList<Entry> list, IList<Entry> appendage)
    {
        foreach(Entry entry in appendage)
        {
            list.Add(entry);
        }
    }

    internal static void AppendDistinct(this IList<Entry> list, IList<Entry> appendage)
    {
        foreach(Entry item in appendage)
        {
            if (!list.Contains(item))
            {
                list.Add(item);
            }
        }
    }

    internal static IEnumerable<Resource> GetResources(this IEnumerable<Entry> entries)
    {
        return entries.Where(entry => entry.HasResource()).Select(entry => entry.Resource);
    }

    // If an interaction has no base, you should be able to supplement it (from the containing bundle for example)
    private static void SupplementBase(this Entry entry, string _base)
    {
        Key key = entry.Key.Clone();
        if (key.HasBase())
            return;
        key.Base = _base;
        entry.Key = key;
    }

    internal static void SupplementBase(this Entry entry, Uri _base)
    {
        entry.SupplementBase(_base.ToString());
    }
}
