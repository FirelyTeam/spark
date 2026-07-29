/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Moq;
using Spark.Engine.Core;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Service;
using Spark.Engine.Service.FhirServiceExtensions;
using System.Net;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Spark.Engine.Tests;

public class FhirServiceTests
{
    [Fact]
    public async Task PatchAsync_WithStaleVersion_ThrowsConflictBeforeApplyingPatch()
    {
        IKey currentKey = Key.Create("Patient", "example", "2");
        Entry current = Entry.PATCH(currentKey, new Patient { Id = "example" });
        var storage = new Mock<IResourceStorageService>(MockBehavior.Strict);
        storage.Setup(service => service.GetAsync(It.Is<IKey>(key =>
            key.TypeName == "Patient" && key.ResourceId == "example" && key.VersionId == null)))
            .ReturnsAsync(current);
        var service = new FhirService(
            Mock.Of<IFhirModel>(),
            [storage.Object],
            Mock.Of<IFhirResponseFactory>());

        SparkException exception = await Assert.ThrowsAsync<SparkException>(() =>
            service.PatchAsync(Key.Create("Patient", "example", "1"), new Parameters()));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
        storage.VerifyAll();
    }
}
