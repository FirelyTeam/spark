/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Engine.Core;
using Spark.Engine.Store;
using Spark.Engine.Store.Interfaces;
using Spark.Store.MongoDB.Search;
using Spark.Store.MongoDB.Search.Common;
using Spark.Store.MongoDB.Search.Indexer;
using Moq;
using System;
using Xunit;

namespace Spark.Store.MongoDB.Tests.Search;

public class MongoSearcherMigrationTests
{
    [Fact]
    public void IncludePlainStringTokenQuery_ReflectsCurrentMigrationState()
    {
        int currentVersion = 0;
        Mock<IDatabaseMigrationService> migrationService = new();
        migrationService
            .Setup(service => service.IsApplied(DatabaseMigrations.StructuredStringTokenIndex.Version))
            .Returns(() => currentVersion >= DatabaseMigrations.StructuredStringTokenIndex.Version);
        MongoSearcher searcher = new(
            new MongoIndexStore("mongodb://localhost/spark", new MongoIndexMapper()),
            new Localhost(new Uri("http://localhost/fhir")),
            new Mock<IFhirModel>().Object,
            referenceNormalizationService: null,
            databaseMigrationService: migrationService.Object);

        Assert.True(searcher.IncludePlainStringTokenQuery);

        currentVersion = 1;

        Assert.False(searcher.IncludePlainStringTokenQuery);
    }

    [Fact]
    public void LegacyConstructor_IncludesPlainStringTokenQuery()
    {
#pragma warning disable CS0618
        MongoSearcher searcher = new(
            new MongoIndexStore("mongodb://localhost/spark", new MongoIndexMapper()),
            new Localhost(new Uri("http://localhost/fhir")),
            new Mock<IFhirModel>().Object,
            null);
#pragma warning restore CS0618

        Assert.True(searcher.IncludePlainStringTokenQuery);
    }
}
