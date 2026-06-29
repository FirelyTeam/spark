/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Engine.Core;
using Spark.Engine.Service.FhirServiceExtensions;
using System;
using System.Linq;
using Xunit;

namespace Spark.Engine.Tests.Service;

public class SnapshotPaginationCalculatorTests
{
    private readonly SnapshotPaginationCalculator _calculator = new SnapshotPaginationCalculator();

    private static Snapshot CreateSnapshot(int totalCount, int? countParam)
    {
        var keys = Enumerable.Range(1, totalCount).Select(i => $"Patient/{i}/_history/1").ToList();
        return Snapshot.Create(
            Hl7.Fhir.Model.Bundle.BundleType.Searchset,
            new Uri("http://localhost/fhir/Patient"),
            keys,
            sortBy: null,
            count: countParam,
            includes: [],
            reverseIncludes: [],
            elements: null);
    }

    // --- GetKeysForPage ---

    [Fact]
    public void GetKeysForPage_WithZeroCountParam_ReturnsEmpty()
    {
        var snapshot = CreateSnapshot(totalCount: 5, countParam: 0);

        var keys = _calculator.GetKeysForPage(snapshot);

        Assert.Empty(keys);
    }

    [Fact]
    public void GetKeysForPage_WithNullCountParam_UsesDefaultPageSize()
    {
        var snapshot = CreateSnapshot(totalCount: 50, countParam: null);

        var keys = _calculator.GetKeysForPage(snapshot);

        Assert.Equal(SnapshotPaginationCalculator.DEFAULT_PAGE_SIZE, keys.Count());
    }

    [Fact]
    public void GetKeysForPage_WithChunkWindow_UsesStartIndex()
    {
        var snapshot = CreateSnapshot(totalCount: 200, countParam: 50);
        var chunk = Snapshot.CreateWindow(snapshot.Id, [snapshot.Split(100)[1]]);

        var keys = _calculator.GetKeysForPage(chunk, start: 120).ToList();

        Assert.Equal(50, keys.Count);
        Assert.Equal("Patient/121/_history/1", keys.First().ToString());
        Assert.Equal("Patient/170/_history/1", keys.Last().ToString());
    }

    // --- GetIndexForLastPage ---

    [Fact]
    public void GetIndexForLastPage_WithZeroCountParam_ReturnsZero()
    {
        var snapshot = CreateSnapshot(totalCount: 10, countParam: 0);

        // Should not throw DivideByZeroException
        int result = _calculator.GetIndexForLastPage(snapshot);

        Assert.Equal(0, result);
    }

    [Fact]
    public void GetIndexForLastPage_WhenAllFitOnOnePage_ReturnsZero()
    {
        var snapshot = CreateSnapshot(totalCount: 5, countParam: 10);

        int result = _calculator.GetIndexForLastPage(snapshot);

        Assert.Equal(0, result);
    }

    [Fact]
    public void GetIndexForLastPage_WithExactlyTwoPages_ReturnsCountParam()
    {
        var snapshot = CreateSnapshot(totalCount: 20, countParam: 10);

        int result = _calculator.GetIndexForLastPage(snapshot);

        Assert.Equal(10, result);
    }

    // --- GetIndexForNextPage ---

    [Fact]
    public void GetIndexForNextPage_WithZeroCountParam_ReturnsNull()
    {
        var snapshot = CreateSnapshot(totalCount: 10, countParam: 0);

        int? result = _calculator.GetIndexForNextPage(snapshot, start: null);

        Assert.Null(result);
    }

    [Fact]
    public void GetIndexForNextPage_OnLastPage_ReturnsNull()
    {
        var snapshot = CreateSnapshot(totalCount: 5, countParam: 5);

        int? result = _calculator.GetIndexForNextPage(snapshot, start: 0);

        Assert.Null(result);
    }

    [Fact]
    public void GetIndexForNextPage_OnFirstPageWithMoreResults_ReturnsCountParam()
    {
        var snapshot = CreateSnapshot(totalCount: 25, countParam: 10);

        int? result = _calculator.GetIndexForNextPage(snapshot, start: 0);

        Assert.Equal(10, result);
    }

    // --- GetIndexForPreviousPage ---

    [Fact]
    public void GetIndexForPreviousPage_WithZeroCountParam_ReturnsNull()
    {
        // When countParam is 0, there is no "previous page" concept either
        var snapshot = CreateSnapshot(totalCount: 10, countParam: 0);

        int? result = _calculator.GetIndexForPreviousPage(snapshot, start: null);

        Assert.Null(result);
    }

    [Fact]
    public void GetIndexForPreviousPage_OnFirstPage_ReturnsNull()
    {
        var snapshot = CreateSnapshot(totalCount: 25, countParam: 10);

        int? result = _calculator.GetIndexForPreviousPage(snapshot, start: 0);

        Assert.Null(result);
    }

    [Fact]
    public void GetIndexForPreviousPage_OnSecondPage_ReturnsZero()
    {
        var snapshot = CreateSnapshot(totalCount: 25, countParam: 10);

        int? result = _calculator.GetIndexForPreviousPage(snapshot, start: 10);

        Assert.Equal(0, result);
    }

    // --- InRange ---

    [Fact]
    public void InRange_WithZeroOffset_ReturnsTrue()
    {
        var snapshot = CreateSnapshot(totalCount: 10, countParam: 10);

        Assert.True(snapshot.InRange(0));
    }

    [Fact]
    public void InRange_WithMiddleOffset_ReturnsTrue()
    {
        var snapshot = CreateSnapshot(totalCount: 10, countParam: 10);

        Assert.True(snapshot.InRange(5));
    }

    [Fact]
    public void InRange_WithOffsetEqualToCount_ReturnsFalse()
    {
        var snapshot = CreateSnapshot(totalCount: 10, countParam: 10);

        Assert.False(snapshot.InRange(10));
    }

    [Fact]
    public void InRange_WithOffsetGreaterThanCount_ReturnsFalse()
    {
        var snapshot = CreateSnapshot(totalCount: 10, countParam: 10);

        Assert.False(snapshot.InRange(11));
    }

    [Fact]
    public void InRange_WithNegativeOffset_ReturnsFalse()
    {
        var snapshot = CreateSnapshot(totalCount: 10, countParam: 10);

        Assert.False(snapshot.InRange(-1));
    }
}
