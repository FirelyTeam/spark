/* 
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;

namespace Spark.Engine.Core;

public class Snapshot
{
    private const int MAX_PAGE_SIZE = 100;

    public string Id { get; set; }
    public Bundle.BundleType Type { get; private init; }
    public IReadOnlyList<string> Keys { get; private init; }
    public string FeedSelfLink { get; private init; }
    public int Count { get; private init; }
    public int? CountParam { get; private init; }
    public bool IsCountOnly { get; private init; }
    public DateTimeOffset WhenCreated { get; private init; }
    public string SortBy { get; private init; }
    public IReadOnlyList<string> Includes  { get; private init; }
    public IReadOnlyList<string> ReverseIncludes  { get; private init; }
    public IReadOnlyList<string> Elements  { get; private init; }
    internal OperationOutcome Outcome { get; private init; }

    public static Snapshot Create(
        Bundle.BundleType type,
        Uri selfLink,
        IReadOnlyList<string> keys,
        string sortBy,
        int? count,
        IReadOnlyList<string> includes,
        IReadOnlyList<string> reverseIncludes,
        IReadOnlyList<string> elements,
        OperationOutcome outcome = null)
    {
        return new Snapshot
        {
            Type = type,
            Id = CreateKey(),
            WhenCreated = DateTimeOffset.UtcNow,
            FeedSelfLink = selfLink.ToString(),
            Includes = includes,
            ReverseIncludes = reverseIncludes,
            Elements = elements,
            Keys = keys,
            Count = keys.Count,
            CountParam = NormalizeCount(count),
            SortBy = sortBy,
            Outcome = outcome,
        };
    }

    internal static Snapshot CreateCountOnly(Bundle.BundleType type, Uri selfLink, long count)
    {
        return new Snapshot
        {
            Type = type,
            Id = CreateKey(),
            WhenCreated = DateTimeOffset.UtcNow,
            FeedSelfLink = selfLink.ToString(),
            Keys = [],
            Count = (int)count,
            IsCountOnly = true,
            Includes = [],
            ReverseIncludes = [],
        };
    }

    private static int? NormalizeCount(int? count)
    {
        return count.HasValue ? Math.Min(count.Value, MAX_PAGE_SIZE) : null;
    }

    private static string CreateKey()
    {
        return Guid.NewGuid().ToString();
    }

    public bool InRange(int index)
    {
        if (index == 0 && Keys.Count == 0)
            return true;
        int last = Keys.Count - 1;
        return (index > 0 || index <= last);
    }
}
