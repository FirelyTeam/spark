/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Search;
using System;
using Xunit;

namespace Spark.Engine.Tests.Extensions;

public class FhirFacadeServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFhirWithMvc_RegistersBothElementIndexerInterfaces()
    {
        ServiceCollection services = new();

        services.AddFhirWithMvc(new SparkSettings { Endpoint = new Uri("http://localhost/fhir") });

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IElementIndexer));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IElementIndexer2));
    }

    [Fact]
    public void AddFhirWithMvc_DoesNotReplaceLegacyElementIndexerRegistration()
    {
        ServiceCollection services = new();
        Mock<IElementIndexer> legacyElementIndexer = new();
        services.AddSingleton(legacyElementIndexer.Object);

        services.AddFhirWithMvc(new SparkSettings { Endpoint = new Uri("http://localhost/fhir") });

        ServiceDescriptor descriptor = Assert.Single(
            services, candidate => candidate.ServiceType == typeof(IElementIndexer));
        Assert.Same(legacyElementIndexer.Object, descriptor.ImplementationInstance);
        Assert.Contains(services, candidate => candidate.ServiceType == typeof(IElementIndexer2));
    }

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
