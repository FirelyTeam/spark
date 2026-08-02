/*
 * Copyright (c) 2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using Moq;
using Spark.Engine.Core;
using Spark.Engine.Model;
using Spark.Engine.Search;
using Spark.Engine.Search.Model;
using Spark.Engine.Search.Types;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Engine.Store.Interfaces;
using System;
using System.Reflection;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Spark.Engine.Tests.Service;

// FIXME: Migrate the old tests in IndexServiceTests to XUnit and Consolidate those tests with these tests.
public class IndexServiceTests2
{
    [Fact]
    public async Task IndexResourceWithContainedReferenceUsesGeneratedIdInParentAndContainedIndexValues()
    {
        FhirModel fhirModel = new();
        Mock<IIndexStore> indexStoreMock = new();
        ElementIndexer elementIndexer = new(fhirModel);
        ResourceResolver resourceResolver = new(fhirModel.SupportedResources, new PocoStructureDefinitionSummaryProvider());
        IndexService indexService = new(fhirModel, indexStoreMock.Object, elementIndexer, resourceResolver);

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
        IndexService indexService = new(fhirModel, indexStoreMock.Object, elementIndexer, resourceResolver);

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
        IndexService indexService = new(fhirModel, indexStoreMock.Object, elementIndexer, resourceResolver);

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
