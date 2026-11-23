/*
 * Copyright (c) 2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Spark.Engine.Core;
using Spark.Engine.Model;
using Spark.Engine.Search;
using Spark.Engine.Search.Model;
using Spark.Engine.Search.Types;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Engine.Store;
using Spark.Engine.Store.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Spark.Engine.Tests.Service;

// FIXME: Migrate the old tests in IndexServiceTests to XUnit and Consolidate those tests with these tests.
public class IndexServiceTests2
{
    [Fact]
    public async Task ProcessBatchAsyncUsesBatchStoreAndMapsFailuresToTheirEntries()
    {
        FhirModel fhirModel = new();
        Mock<IIndexStore2> indexStoreMock = new();
        ElementIndexer elementIndexer = new(fhirModel);
        ResourceResolver resourceResolver = new(fhirModel.SupportedResources, new PocoStructureDefinitionSummaryProvider());
        IndexService indexService = new(
            fhirModel,
            indexStoreMock.Object,
            elementIndexer,
            resourceResolver,
            new NullLogger<IndexService>()
        );
        Entry firstEntry = Entry.Create(Key.Create("Patient", "patient-1"), new Patient { Id = "patient-1" });
        Entry secondEntry = Entry.Create(Key.Create("Patient", "patient-2"), new Patient { Id = "patient-2" });
        InvalidOperationException writeException = new("Bulk write failed.");
        indexStoreMock
            .Setup(store => store.SaveBatchAsync(It.IsAny<IReadOnlyList<IndexValue>>()))
            .ReturnsAsync([new IndexStoreWriteFailure { IndexValueIndex = 1, Exception = writeException }]);

        IReadOnlyList<IndexBatchFailure> failures = await indexService.ProcessBatchAsync([firstEntry, secondEntry]);

        IndexBatchFailure failure = Assert.Single(failures);
        Assert.Same(secondEntry, failure.Entry);
        Assert.Same(writeException, failure.Exception);
        indexStoreMock.Verify(
            store => store.SaveBatchAsync(It.Is<IReadOnlyList<IndexValue>>(values => values.Count == 2)),
            Times.Once
        );
        indexStoreMock.Verify(store => store.SaveAsync(It.IsAny<IndexValue>()), Times.Never);
    }

    [Fact]
    public async Task IndexResourceWithContainedReferenceUsesGeneratedIdInParentAndContainedIndexValues()
    {
        FhirModel fhirModel = new();
        Mock<IIndexStore> indexStoreMock = new();
        ElementIndexer elementIndexer = new(fhirModel);
        ResourceResolver resourceResolver = new(fhirModel.SupportedResources, new PocoStructureDefinitionSummaryProvider());
        IndexService indexService = new(fhirModel, indexStoreMock.Object, elementIndexer, resourceResolver, new NullLogger<IndexService>());

        Organization containedOrganization = new() { Id = "contained" };
        Organization organization = new()
        {
            Id = "parent",
            PartOf = new ResourceReference("#contained")
        };
        organization.Contained.Add(containedOrganization);

        IndexValue indexValue = await indexService.IndexResourceAsync(
            organization,
            Key.Create("Organization", "parent"));

        IndexValue containedIndex = Assert.Single(
            indexValue.IndexValues(), value => value.Name == "contained");
        IndexValue containedJustId = Assert.Single(
            containedIndex.IndexValues(), value => value.Name == IndexFieldNames.JUSTID);
        string indexedContainedId = Assert.IsType<StringValue>(
            Assert.Single(containedJustId.Values)).Value;

        IndexValue partOfIndex = Assert.Single(
            indexValue.IndexValues(), value => value.Name == "partof");
        string indexedPartOf = Assert.IsType<StringValue>(
            Assert.Single(partOfIndex.Values)).Value;

        Assert.NotEqual("contained", indexedContainedId);
        Assert.True(Guid.TryParse(indexedContainedId, out _));
        Assert.Equal($"Organization/{indexedContainedId}", indexedPartOf);
        Assert.Equal("contained", containedOrganization.Id);
        Assert.Equal("#contained", organization.PartOf.Reference);
    }

    [Fact]
    public void MakeContainedReferencesUniqueCopiesAndRewritesOnlyTheIndexedResource()
    {
        FhirModel fhirModel = new();
        Mock<IIndexStore> indexStoreMock = new();
        ElementIndexer elementIndexer = new(fhirModel);
        ResourceResolver resourceResolver = new(fhirModel.SupportedResources, new PocoStructureDefinitionSummaryProvider());
        IndexService indexService = new(fhirModel, indexStoreMock.Object, elementIndexer, resourceResolver, new NullLogger<IndexService>());

        Organization containedOrganization = new() { Id = "contained" };
        Organization organization = new()
        {
            Id = "parent",
            PartOf = new ResourceReference("#contained")
        };
        organization.Contained.Add(containedOrganization);

        MethodInfo method = typeof(IndexService).GetMethod(
            "MakeContainedReferencesUnique",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Organization indexedOrganization = Assert.IsType<Organization>(
            method.Invoke(indexService, new object[] { organization }));
        Organization indexedContainedOrganization = Assert.Single(indexedOrganization.Contained) as Organization;

        Assert.NotSame(organization, indexedOrganization);
        Assert.NotSame(containedOrganization, indexedContainedOrganization);

        Assert.Equal("contained", containedOrganization.Id);
        Assert.Equal("#contained", organization.PartOf.Reference);

        Assert.NotEqual("contained", indexedContainedOrganization.Id);
        Assert.True(Guid.TryParse(indexedContainedOrganization.Id, out _));
        Assert.Equal($"Organization/{indexedContainedOrganization.Id}", indexedOrganization.PartOf.Reference);
    }

    [Fact]
    public async Task IndexResourceWithContainedResourcesLackingAnIdShouldNotCrash()
    {
        FhirModel fhirModel = new();
        Mock<IIndexStore> indexStoreMock = new();
        ElementIndexer elementIndexer = new(fhirModel);
        ResourceResolver resourceResolver = new(fhirModel.SupportedResources, new PocoStructureDefinitionSummaryProvider());
        IndexService indexService = new(fhirModel, indexStoreMock.Object, elementIndexer, resourceResolver, new NullLogger<IndexService>());

        Organization organization = new()
        {
            Name = "An Organization", Identifier = { new Identifier("http://a-fake-system", "a value") }
        };

        organization.Contained.Add(new Endpoint
        {
            Identifier = { new Identifier { System = "http://not-a-real-system", Value = "endpoint-1-identifier" } }
        });
        organization.Contained.Add(new Endpoint
        {
            Identifier = { new Identifier { System = "http://not-a-real-system", Value = "endpoint-2-identifier" } }
        });

        Key key = Key.Create(organization.TypeName, organization.Id);
        await indexService.IndexResourceAsync(organization, key);
    }
}
