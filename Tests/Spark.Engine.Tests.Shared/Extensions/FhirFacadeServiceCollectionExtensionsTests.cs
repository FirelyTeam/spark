/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.FhirResponseFactory;
using System;
using Xunit;

namespace Spark.Engine.Tests.Extensions;

public class FhirFacadeServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFhirFacadeCore_RegistersFacadeServices_WithoutControllerServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFhirModel, FhirModel>();

        services.AddFhirFacadeCore(options =>
        {
            options.Settings.Endpoint = new Uri("http://localhost/fhir");
        });

        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(ApplicationPartManager));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IActionInvokerFactory));

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<SparkSettings>());
        Assert.NotNull(provider.GetRequiredService<StoreSettings>());
        Assert.NotNull(provider.GetRequiredService<ILocalhost>());
        Assert.NotNull(provider.GetRequiredService<IFhirResponseFactory>());
        Assert.NotNull(provider.GetRequiredService<BaseFhirJsonDeserializer>());
        Assert.NotNull(provider.GetRequiredService<BaseFhirXmlDeserializer>());
        Assert.NotNull(provider.GetRequiredService<BaseFhirJsonSerializer>());
        Assert.NotNull(provider.GetRequiredService<BaseFhirXmlSerializer>());
    }
}
