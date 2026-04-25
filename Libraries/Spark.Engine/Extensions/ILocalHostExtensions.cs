/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2020-2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Spark.Engine.Core;

namespace Spark.Engine.Extensions;

internal static class ILocalHostExtensions
{
    private static Bundle.HTTPVerb DetermineMethod(this ILocalhost localhost, IKey key)
    {
        if (key == null)
            return Bundle.HTTPVerb.DELETE; // probably...

        return localhost.GetKeyKind(key) switch
        {
            KeyKind.Foreign or KeyKind.Temporary => Bundle.HTTPVerb.POST,
            _ => Bundle.HTTPVerb.PUT,
        };
    }

    internal static Key ExtractKey(this ILocalhost localhost, Bundle.EntryComponent entry)
    {
        Key key = null;
        if (entry.Request is { Url: not null })
        {
            key = localhost.UriToKey(entry.Request.Url);
        }
        else if (entry.Resource != null)
        {
            key = entry.Resource.ExtractKey();
        }
        if (key != null && string.IsNullOrEmpty(key.ResourceId)
                        && entry.FullUrl != null && UriHelper.IsTemporaryUri(entry.FullUrl))
        {
            key.ResourceId = entry.FullUrl;
        }
        return key;
    }

    internal static Bundle.HTTPVerb ExtrapolateMethod(this ILocalhost localhost, Bundle.EntryComponent entry, IKey key)
    {
        return entry.Request?.Method ?? localhost.DetermineMethod(key);
    }

    internal static Entry ToInteraction(this ILocalhost localhost, Bundle.EntryComponent bundleEntry)
    {
        Key key = localhost.ExtractKey(bundleEntry);
        Bundle.HTTPVerb method = localhost.ExtrapolateMethod(bundleEntry, key);

        return key != null
            ? Entry.Create(method, key, bundleEntry.Resource)
            : Entry.Create(method, bundleEntry.Resource);
    }
}
