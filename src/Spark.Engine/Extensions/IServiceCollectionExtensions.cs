/*
 * Copyright (c) 2019-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Spark.Engine.Core;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Filters;
using Spark.Engine.Formatters;
using Spark.Engine.Interfaces;
using Spark.Engine.Search;
using Spark.Engine.Service;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Engine.Store.Interfaces;
using Spark.Engine.Utility;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Engine.Extensions;

public static class IServiceCollectionExtensions
{
    public static void AddStore<TService>(this IServiceCollection services, StoreSettings settings)
        where TService : class, IFhirStore
    {
        services.TryAddSingleton(settings);
        services.TryAddTransient<TService>();
    }

    public static void AddStore<TService, TImplementation>(this IServiceCollection services, StoreSettings settings) 
        where TService : class, IFhirStore
        where TImplementation : class, TService
    {
        services.TryAddSingleton(settings);
        services.TryAddTransient<TService, TImplementation>();
    }

    public static void AddIdGenerator<T>(this IServiceCollection services)
        where T : class, IIdentityGenerator
    {
        services.TryAddTransient<IIdentityGenerator, T>();
    }

    internal static void AddFhirExtensions(this IServiceCollection services, IDictionary<Type, Type> fhirServiceExtensions)
    {
        foreach (var fhirServiceExtension in fhirServiceExtensions)
        {
            services.AddTransient(fhirServiceExtension.Key, fhirServiceExtension.Value);
        }

        services.AddTransient((provider) => provider.GetFhirExtensions(fhirServiceExtensions).ToArray());
    }

    internal static IEnumerable<IFhirServiceExtension> GetFhirExtensions(this IServiceProvider provider, IDictionary<Type, Type> fhirServiceExtensions)
    {
        yield return provider.GetRequiredService<IResourceStorageService>();
        foreach (var fhirServiceExtension in fhirServiceExtensions)
        {
            yield return (IFhirServiceExtension) provider.GetRequiredService(fhirServiceExtension.Key);
        }
    }

    private static DeserializerSettings GetDeserializerSettings(SparkSettings settings) =>
        settings.DeserializerSettings ?? DeserializerSettingsFactory.GetStrictDeserializerSettings();

    public static IMvcBuilder AddFhirFacade(this IServiceCollection services, Action<SparkOptions> options)
    {
        services.Configure(options);

        var serviceProvider = services.BuildServiceProvider();
        var opts = serviceProvider.GetRequiredService<IOptions<SparkOptions>>()?.Value;
        var settings = opts.Settings;

        services.AddSingleton(settings);
        services.AddSingleton(opts.StoreSettings);

        foreach (KeyValuePair<Type,Type> fhirService in opts.FhirServices)
        {
            services.AddSingleton(fhirService.Key, fhirService.Value);
        }

        foreach (var fhirStore in opts.FhirStores)
        {
            services.AddTransient(fhirStore.Key, fhirStore.Value);
        }

        services.AddTransient<ILocalhost>(_ => new Localhost(opts.Settings?.Endpoint));

        services.TryAddTransient<ITransfer, Transfer>();
        services.TryAddTransient<ConditionalHeaderFhirResponseInterceptor>();
        services.TryAddTransient(provider => new IFhirResponseInterceptor[] { provider.GetRequiredService<ConditionalHeaderFhirResponseInterceptor>() });
        services.TryAddTransient<IFhirResponseInterceptorRunner, FhirResponseInterceptorRunner>();
        services.TryAddTransient<IFhirResponseFactory, FhirResponseFactory.FhirResponseFactory>();
        services.TryAddTransient<ICompositeServiceListener, ServiceListener>();
        services.TryAddTransient<ResourceJsonInputFormatter>();
        services.TryAddTransient<ResourceJsonOutputFormatter>();
        services.TryAddTransient<ResourceXmlInputFormatter>();
        services.TryAddTransient<ResourceXmlOutputFormatter>();

        if (services.IndexOf(new ServiceDescriptor(typeof(IResourceStorageService), typeof(ResourceStorageService), ServiceLifetime.Transient)) == -1)
        {
            services.TryAddTransient<IResourceStorageService, ResourceStorageService>();
        }

        services.AddFhirExtensions(opts.FhirExtensions);

        services.TryAddTransient(provider =>  provider.GetServices<IServiceListener>().ToArray());

        services.TryAddSingleton(provider => new BaseFhirJsonDeserializer(provider.GetRequiredService<IFhirModel>().GetModelInspector(), GetDeserializerSettings(settings)));
        services.TryAddSingleton(provider => new BaseFhirXmlDeserializer(provider.GetRequiredService<IFhirModel>().GetModelInspector(), GetDeserializerSettings(settings)));
        services.TryAddSingleton(provider => new BaseFhirJsonSerializer(provider.GetRequiredService<IFhirModel>().GetModelInspector()));
        services.TryAddSingleton(provider => new BaseFhirXmlSerializer(provider.GetRequiredService<IFhirModel>().GetModelInspector()));

        return services.AddFhirFormatters(settings, opts.MvcOption);
    }

    internal static IMvcBuilder AddFhirInternal(this IServiceCollection services, SparkSettings settings, Action<MvcOptions> setupAction = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        services.SetContentTypeAsFhirMediaTypeOnValidationError();

        services.TryAddSingleton<SparkSettings>(settings);
        services.TryAddTransient<ElementIndexer>();

        services.TryAddTransient<IReferenceNormalizationService, ReferenceNormalizationService>();

        services.TryAddSingleton(provider =>
            new ResourceResolver(provider.GetRequiredService<IFhirModel>().SupportedResources));

        services.TryAddTransient<IIndexService, IndexService>();
        services.TryAddTransient<ILocalhost>(_ => new Localhost(settings.Endpoint));
        services.TryAddTransient<ITransfer, Transfer>();
        services.TryAddTransient<ConditionalHeaderFhirResponseInterceptor>();
        services.TryAddTransient(provider => new IFhirResponseInterceptor[] { provider.GetRequiredService<ConditionalHeaderFhirResponseInterceptor>() });
        services.TryAddTransient<IFhirResponseInterceptorRunner, FhirResponseInterceptorRunner>();
        services.TryAddTransient<IFhirResponseFactory, FhirResponseFactory.FhirResponseFactory>();
        services.TryAddTransient<IIndexRebuildService, IndexRebuildService>();
        services.TryAddTransient<ISearchService, SearchService>();
        services.TryAddTransient<ISnapshotPaginationProvider, SnapshotPaginationProvider>();
        services.TryAddTransient<ISnapshotPaginationCalculator, SnapshotPaginationCalculator>();
        if (settings.Experimental.IndexingMode == IndexingMode.Background)
        {
            services.TryAddTransient<IServiceListener, IndexQueueEnqueueListener>();
            services.AddHostedService<IndexWorker>();
        }
        else
        {
            services.TryAddTransient<IServiceListener, IndexServiceListener>();
        }
        services.TryAddTransient(provider => new IServiceListener[] { provider.GetRequiredService<IServiceListener>() });
        services.TryAddTransient<SearchService>();                     // search
        services.TryAddTransient<ITransactionService, TransactionService>();  // transaction
        services.TryAddTransient<HistoryService>();                    // history
        services.TryAddTransient<PagingService>();                     // paging
        services.TryAddTransient<ResourceStorageService>();            // storage
        services.TryAddTransient<PatchService>();           // patch
        services.TryAddTransient<ICompositeServiceListener, ServiceListener>();
        services.TryAddTransient<ResourceJsonInputFormatter>();
        services.TryAddTransient<ResourceJsonOutputFormatter>();
        services.TryAddTransient<ResourceXmlInputFormatter>();
        services.TryAddTransient<ResourceXmlOutputFormatter>();

        services.AddTransient((provider) => new IFhirServiceExtension[]
        {
            provider.GetRequiredService<SearchService>(),
            provider.GetRequiredService<ITransactionService>(),
            provider.GetRequiredService<HistoryService>(),
            provider.GetRequiredService<PagingService>(),
            provider.GetRequiredService<ResourceStorageService>(),
            provider.GetRequiredService<PatchService>(),
        });

        services.TryAddSingleton(provider => new BaseFhirJsonDeserializer(provider.GetRequiredService<IFhirModel>().GetModelInspector(), GetDeserializerSettings(settings)));
        services.TryAddSingleton(provider => new BaseFhirXmlDeserializer(provider.GetRequiredService<IFhirModel>().GetModelInspector(), GetDeserializerSettings(settings)));
        services.TryAddSingleton(provider => new BaseFhirJsonSerializer(provider.GetRequiredService<IFhirModel>().GetModelInspector()));
        services.TryAddSingleton(provider => new BaseFhirXmlSerializer(provider.GetRequiredService<IFhirModel>().GetModelInspector()));

        services.TryAddSingleton<IFhirService, FhirService>();

        var builder = services.AddFhirFormatters(settings, setupAction);

        services.RemoveAll<OutputFormatterSelector>();
        services.TryAddSingleton<OutputFormatterSelector, FhirOutputFormatterSelector>();

        services.RemoveAll<OutputFormatterSelector>();
        services.TryAddSingleton<OutputFormatterSelector, FhirOutputFormatterSelector>();

        return builder;
    }

    public static IMvcBuilder AddFhirFormatters(this IServiceCollection services, SparkSettings settings, Action<MvcOptions> setupAction = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return services.AddControllers(options =>
        {
            var serviceProvider = services.BuildServiceProvider();

            options.Filters.Add<UnsupportedMediaTypeFilter>(-3001);
            // Suppress recursive child-property validation for FHIR Resource types.
            // ASP.NET's ValidationVisitor triggers property getters (e.g. Attachment.get_Size())
            // that throw InvalidCastException in Firely 6 due to internal type changes.
            // Non-FHIR controllers are unaffected.
            options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(Resource)));

            if (settings.UseAsynchronousIO)
            {
                options.InputFormatters.Insert(0,new AsyncResourceJsonInputFormatter(serviceProvider.GetRequiredService<BaseFhirJsonDeserializer>()));
                options.InputFormatters.Insert(1,new AsyncResourceXmlInputFormatter(serviceProvider.GetRequiredService<BaseFhirXmlDeserializer>()));
                options.InputFormatters.Insert(2,new BinaryInputFormatter());
                options.OutputFormatters.Insert(0,new AsyncResourceJsonOutputFormatter(serviceProvider.GetRequiredService<BaseFhirJsonSerializer>()));
                options.OutputFormatters.Insert(1,new AsyncResourceXmlOutputFormatter(serviceProvider.GetRequiredService<BaseFhirXmlSerializer>()));
                options.OutputFormatters.Insert(2,new BinaryOutputFormatter());
            }
            else
            {
                options.InputFormatters.Insert(0,new ResourceJsonInputFormatter(serviceProvider.GetRequiredService<BaseFhirJsonDeserializer>(), ArrayPool<char>.Shared));
                options.InputFormatters.Insert(1,new ResourceXmlInputFormatter(serviceProvider.GetRequiredService<BaseFhirXmlDeserializer>()));
                options.InputFormatters.Insert(2,new BinaryInputFormatter());
                options.OutputFormatters.Insert(0,new ResourceJsonOutputFormatter(serviceProvider.GetRequiredService<BaseFhirJsonSerializer>()));
                options.OutputFormatters.Insert(1,new ResourceXmlOutputFormatter(serviceProvider.GetRequiredService<BaseFhirXmlSerializer>()));
                options.OutputFormatters.Insert(2,new BinaryOutputFormatter());
            }

            options.RespectBrowserAcceptHeader = true;

            setupAction?.Invoke(options);
        });
    }

    public static void AddCustomSearchParameters(this IServiceCollection services, IEnumerable<SearchParamDefinition> searchParameters)
    {
        // Add any user-supplied SearchParameters
        ModelInfo.SearchParameters.AddRange(searchParameters);
    }

    private static void SetContentTypeAsFhirMediaTypeOnValidationError(this IServiceCollection services)
    {
        // Validation errors need to be returned as application/json or application/xml
        // instead of application/problem+json and application/problem+xml.
        // (https://github.com/FirelyTeam/spark/issues/282)
        services.Configure<ApiBehaviorOptions>(options =>
        {
            var defaultInvalidModelStateResponseFactory = options.InvalidModelStateResponseFactory;
            options.InvalidModelStateResponseFactory = context =>
            {
                var actionResult = defaultInvalidModelStateResponseFactory(context) as ObjectResult;
                if (actionResult != null)
                {
                    actionResult.ContentTypes.Clear();
                    foreach (var mediaType in FhirMediaType.JsonMimeTypes
                                 .Concat(FhirMediaType.XmlMimeTypes))
                    {
                        actionResult.ContentTypes.Add(mediaType);
                    }
                }
                return actionResult;
            };
        });
    }
}
