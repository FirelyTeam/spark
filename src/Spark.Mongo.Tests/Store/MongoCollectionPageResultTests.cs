/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using MongoDB.Driver;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;
using Spark.Mongo.Store;
using Spark.Store.Mongo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Testcontainers.MongoDb;
using Xunit;
using Assert = Xunit.Assert;
using Task = System.Threading.Tasks.Task;

namespace Spark.Mongo.Tests.Store;

[Trait("Category", "Integration")]
public class MongoCollectionPageResultTests
{
    [SkippableFact]
    public async Task IterateAllPagesAsync_Visits_Every_Current_Entry_Once_In_Pages()
    {
        Skip.IfNot(OperatingSystem.IsLinux());

        var container = await StartMongoOrSkipAsync();
        try
        {
            var connectionString = BuildConnectionString(container.GetConnectionString(), "pageresulttest");
            var store = new MongoFhirStore(connectionString);
            var expectedIds = Enumerable.Range(1, 25).Select(i => $"p{i:D2}").ToList();
            foreach (var id in expectedIds)
            {
                await store.AddAsync(Entry.POST(Key.Create("Patient", id, "1"), new Patient { Id = id }));
            }

            IPageResult<Entry> pages = await new MongoFhirStorePagedReader(connectionString)
                .ReadAsync(new FhirStorePageReaderOptions { PageSize = 10 });

            var pageSizes = new List<int>();
            var visitedIds = new List<string>();
            await pages.IterateAllPagesAsync(entries =>
            {
                pageSizes.Add(entries.Count);
                visitedIds.AddRange(entries.Select(entry => entry.Key.ResourceId));
                return Task.CompletedTask;
            });

            Assert.Equal(25, pages.TotalRecords);
            Assert.Equal(new[] { 10, 10, 5 }, pageSizes);
            Assert.Equal(expectedIds, visitedIds);
        }
        finally
        {
            await container.DisposeAsync();
        }
    }

    private static async System.Threading.Tasks.Task<MongoDbContainer> StartMongoOrSkipAsync()
    {
        MongoDbContainer container = null;
        try
        {
            container = new MongoDbBuilder("mongo:8.2.7").Build();
            await container.StartAsync(CancellationToken.None);
            return container;
        }
        catch (Exception ex)
        {
            if (container != null)
            {
                await container.DisposeAsync();
            }

            Skip.If(true, $"Docker/Testcontainers not available: {ex.Message}");
            return null;
        }
    }

    private static string BuildConnectionString(string raw, string databaseName)
    {
        var builder = new MongoUrlBuilder(raw) { DatabaseName = databaseName };
        if (!string.IsNullOrEmpty(builder.Username) && string.IsNullOrEmpty(builder.AuthenticationSource))
        {
            builder.AuthenticationSource = "admin";
        }

        return builder.ToMongoUrl().ToString();
    }
}
