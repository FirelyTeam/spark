/*
 * Copyright (c) 2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Moq;
using Spark.Engine.Core;
using Spark.Engine.Search;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Engine.Store.Interfaces;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Spark.Engine.Test.Service;

// FIXME: Migrate the old tests in IndexServiceTests to XUnit and Consolidate those tests with these tests.
public class IndexServiceTests2
{
    [Fact]
    public async Task IndexResourceWithContainedResourcesLackingAnIdShouldNotCrash()
    {
        FhirModel fhirModel = new();
        Mock<IIndexStore> indexStoreMock = new();
        ElementIndexer elementIndexer = new(fhirModel);
        ResourceResolver resourceResolver = new();
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
