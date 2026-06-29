/* 
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Engine.Core;

public class Snapshot
{
    private const int MAX_PAGE_SIZE = 100;
    public const int DEFAULT_PAGE_SIZE = 20;

    public string Id { get; set; }
    public string GroupId { get; private init; }
    public int StartIndex { get; private init; }
    public int KeyCount { get; private init; }
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
            KeyCount = keys.Count,
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
            KeyCount = 0,
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

    public int GetPageSize()
    {
        return CountParam ?? DEFAULT_PAGE_SIZE;
    }

    public IReadOnlyList<Snapshot> Split(int maxKeyCount)
    {
        if (maxKeyCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxKeyCount), "The snapshot chunk size must be greater than zero.");

        if (IsCountOnly || Keys.Count <= maxKeyCount)
            return [this];

        var chunks = new List<Snapshot>();
        for (var startIndex = 0; startIndex < Keys.Count; startIndex += maxKeyCount)
        {
            var keys = Keys.Skip(startIndex).Take(maxKeyCount).ToList();
            chunks.Add(new Snapshot
            {
                Id = CreateKey(),
                GroupId = Id,
                StartIndex = startIndex,
                KeyCount = keys.Count,
                Type = Type,
                WhenCreated = WhenCreated,
                FeedSelfLink = FeedSelfLink,
                Includes = Includes,
                ReverseIncludes = ReverseIncludes,
                Elements = Elements,
                Keys = keys,
                Count = Count,
                CountParam = CountParam,
                IsCountOnly = IsCountOnly,
                SortBy = SortBy,
                Outcome = Outcome,
            });
        }

        return chunks;
    }

    public static Snapshot CreateWindow(string snapshotGroupId, IReadOnlyList<Snapshot> chunks)
    {
        if (chunks == null || chunks.Count == 0)
            return null;

        var ordered = chunks.OrderBy(chunk => chunk.StartIndex).ToList();
        var first = ordered[0];
        var keys = ordered.SelectMany(chunk => chunk.Keys).ToList();
        var groupId = snapshotGroupId ?? first.GroupId ?? first.Id;

        return new Snapshot
        {
            Id = groupId,
            GroupId = first.GroupId,
            StartIndex = first.StartIndex,
            KeyCount = keys.Count,
            Type = first.Type,
            WhenCreated = first.WhenCreated,
            FeedSelfLink = first.FeedSelfLink,
            Includes = first.Includes,
            ReverseIncludes = first.ReverseIncludes,
            Elements = first.Elements,
            Keys = keys,
            Count = first.Count,
            CountParam = first.CountParam,
            IsCountOnly = first.IsCountOnly,
            SortBy = first.SortBy,
            Outcome = first.Outcome,
        };
    }

    public bool InRange(int index)
    {
        if (index < 0)
            return false;
        if (index == 0)
            return true;
        return index < Count;
    }
}
