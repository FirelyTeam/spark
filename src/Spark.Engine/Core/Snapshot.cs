/* 
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2024, Incendi <info@incendi.no>
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
    public const int NOCOUNT = -1;
    public const int MAX_PAGE_SIZE = 100;
    public const int DEFAULT_PAGE_SIZE = 20;

    public string Id { get; set; }
    public string GroupId { get; set; }
    public int StartIndex { get; set; }
    public int KeyCount { get; set; }
    public Bundle.BundleType Type { get; set; }
    public IReadOnlyList<string> Keys { get; set; }
    public string FeedSelfLink { get; set; }
    public int Count { get; set; }
    public int? CountParam { get; set; }
    public DateTimeOffset WhenCreated;
    public string SortBy { get; set; }
    public IReadOnlyList<string> Includes;
    public IReadOnlyList<string> ReverseIncludes;
    public IReadOnlyList<string> Elements;
    internal OperationOutcome Outcome { get; private set; }

    public static Snapshot Create(
        Bundle.BundleType type,
        Uri selflink,
        IReadOnlyList<string> keys,
        string sortby,
        int? count,
        IReadOnlyList<string> includes,
        IReadOnlyList<string> reverseIncludes,
        IReadOnlyList<string> elements,
        OperationOutcome outcome = null)
    {
        Snapshot snapshot = new()
        {
            Type = type,
            Id = CreateKey(),
            WhenCreated = DateTimeOffset.UtcNow,
            FeedSelfLink = selflink.ToString(),
            Includes = includes,
            ReverseIncludes = reverseIncludes,
            Elements = elements,
            Keys = keys,
            Count = keys.Count(),
            KeyCount = keys.Count(),
            CountParam = NormalizeCount(count),
            SortBy = sortby,
            Outcome = outcome,
        };
        return snapshot;
    }

    private static int? NormalizeCount(int? count)
    {
        if (count.HasValue)
        {
            return Math.Min(count.Value, MAX_PAGE_SIZE);
        }
        return count;
    }

    public static string CreateKey()
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

        if (Keys.Count <= maxKeyCount)
            return new[] { this };

        List<Snapshot> chunks = new();
        for (int startIndex = 0; startIndex < Keys.Count; startIndex += maxKeyCount)
        {
            List<string> keys = Keys.Skip(startIndex).Take(maxKeyCount).ToList();
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

        List<Snapshot> ordered = chunks.OrderBy(chunk => chunk.StartIndex).ToList();
        Snapshot first = ordered[0];
        List<string> keys = ordered.SelectMany(chunk => chunk.Keys).ToList();
        string groupId = snapshotGroupId ?? first.GroupId ?? first.Id;

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

public static class SnapshotExtensions
{
    [Obsolete("Method will be removed in a future version")]
    public static IEnumerable<string> Keys(this Bundle bundle)
    {
        return bundle.GetResources().Keys();
    }

    public static IEnumerable<string> Keys(this IEnumerable<Resource> resources)
    {
        return resources.Select(e => e.VersionId);
    }
}
