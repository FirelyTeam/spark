/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.Extensions.Logging.Abstractions;
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
    public void MigrationState_ReflectsCurrentMigrationState()
    {
        int currentVersion = 0;
        Mock<IDatabaseMigrationService> migrationService = new();
        migrationService
            .Setup(service => service.IsApplied(DatabaseMigrations.StructuredStringTokenIndex.Version))
            .Returns(() => currentVersion >= DatabaseMigrations.StructuredStringTokenIndex.Version);
        MongoSearcher searcher = new(
            new MongoIndexStore("mongodb://localhost/spark", new MongoIndexMapper(), new NullLogger<MongoIndexStore>()),
            new Localhost(new Uri("http://localhost/fhir")),
            new Mock<IFhirModel>().Object,
            referenceNormalizationService: null,
            databaseMigrationService: migrationService.Object
        );

        Assert.Equal(SearchIndexMigrationState.None, searcher.MigrationState);

        currentVersion = 1;

        Assert.Equal(SearchIndexMigrationState.StructuredStringTokenIndex, searcher.MigrationState);
    }

    [Fact]
    public void LegacyConstructor_UsesLegacyMigrationState()
    {
#pragma warning disable CS0618
        MongoSearcher searcher = new(
            new MongoIndexStore("mongodb://localhost/spark", new MongoIndexMapper(), new NullLogger<MongoIndexStore>()),
            new Localhost(new Uri("http://localhost/fhir")),
            new Mock<IFhirModel>().Object,
            null
        );
#pragma warning restore CS0618

        Assert.Equal(SearchIndexMigrationState.None, searcher.MigrationState);
    }
}
