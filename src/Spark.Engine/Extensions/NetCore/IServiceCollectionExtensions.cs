/*
 * Copyright (c) 2019-2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

#if NETSTANDARD2_0
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Filters;
using Spark.Engine.Formatters;
using Spark.Engine.Interfaces;
using Spark.Engine.Search;
using Spark.Engine.Service;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Engine.Store.Interfaces;
using Spark.Service;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Engine.Extensions
{
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
            where T : class, IGenerator
        {
            services.TryAddTransient<IGenerator, T>();
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

        public static IMvcCoreBuilder AddFhirFacade(this IServiceCollection services, Action<SparkOptions> options)
        {
            services.Configure(options);

            var provider = services.BuildServiceProvider();
            var opts = provider.GetRequiredService<IOptions<SparkOptions>>()?.Value;
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

            services.AddTransient<ILocalhost>(provider => new Localhost(opts.Settings?.Endpoint));

            services.TryAddTransient<IFhirModel>((provider) => new FhirModel(ModelInfo.SearchParameters));
            services.TryAddTransient((provider) => new FhirPropertyIndex(provider.GetRequiredService<IFhirModel>()));
            services.TryAddTransient<ITransfer, Transfer>();
            services.TryAddTransient<ConditionalHeaderFhirResponseInterceptor>();
            services.TryAddTransient((provider) => new IFhirResponseInterceptor[] { provider.GetRequiredService<ConditionalHeaderFhirResponseInterceptor>() });
            services.TryAddTransient<IFhirResponseInterceptorRunner, FhirResponseInterceptorRunner>();
            services.TryAddTransient<IFhirResponseFactory, FhirResponseFactory.FhirResponseFactory>();
            services.TryAddTransient<ICompositeServiceListener, ServiceListener>();
            services.TryAddTransient<ResourceJsonInputFormatter>();
            services.TryAddTransient<ResourceJsonOutputFormatter>();
            services.TryAddTransient<ResourceXmlInputFormatter>();
            services.TryAddTransient<ResourceXmlOutputFormatter>();
            services.TryAddTransient<ResourceStorageService>();
            services.TryAddTransient<IResourceStorageService>(s => s.GetRequiredService<ResourceStorageService>());
            services.TryAddTransient<IAsyncResourceStorageService>(s => s.GetRequiredService<ResourceStorageService>());

            services.AddFhirExtensions(opts.FhirExtensions);

            services.TryAddTransient(provider =>  provider.GetServices<IServiceListener>().ToArray());

            services.TryAddSingleton(provider => new FhirJsonParser(settings.ParserSettings));
            services.TryAddSingleton(provider => new FhirXmlParser(settings.ParserSettings));
            services.TryAddSingleton(provder => new FhirJsonSerializer(settings.SerializerSettings));
            services.TryAddSingleton(provder => new FhirXmlSerializer(settings.SerializerSettings));

            IMvcCoreBuilder builder = services.AddFhirFormatters(settings, opts.MvcOption);

            return builder;
        }

        public static IMvcCoreBuilder AddFhir(this IServiceCollection services, SparkSettings settings, Action<MvcOptions> setupAction = null)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            services.AddFhirHttpSearchParameters();

            services.SetContentTypeAsFhirMediaTypeOnValidationError();

            services.TryAddSingleton<SparkSettings>(settings);
            services.TryAddTransient<ElementIndexer>();

            services.TryAddTransient<IReferenceNormalizationService, ReferenceNormalizationService>();

            services.TryAddTransient<IndexService>();
            services.TryAddTransient<IIndexService>(s => s.GetRequiredService<IndexService>());
            services.TryAddTransient<IAsyncIndexService>(s => s.GetRequiredService<IndexService>());
            services.TryAddTransient<ILocalhost>((provider) => new Localhost(settings.Endpoint));
            services.TryAddTransient<IFhirModel>((provider) => new FhirModel(ModelInfo.SearchParameters));
            services.TryAddTransient((provider) => new FhirPropertyIndex(provider.GetRequiredService<IFhirModel>()));
            services.TryAddTransient<ITransfer, Transfer>();
            services.TryAddTransient<ConditionalHeaderFhirResponseInterceptor>();
            services.TryAddTransient((provider) => new IFhirResponseInterceptor[] { provider.GetRequiredService<ConditionalHeaderFhirResponseInterceptor>() });
            services.TryAddTransient<IFhirResponseInterceptorRunner, FhirResponseInterceptorRunner>();
            services.TryAddTransient<IFhirResponseFactory, FhirResponseFactory.FhirResponseFactory>();
            services.TryAddTransient<IIndexRebuildService, IndexRebuildService>();
            services.TryAddTransient<SearchService>();
            services.TryAddTransient<ISearchService>(s => s.GetRequiredService<SearchService>());
            services.TryAddTransient<IAsyncSearchService>(s => s.GetRequiredService<SearchService>());
            services.TryAddTransient<SnapshotPaginationProvider>();
            services.TryAddTransient<ISnapshotPaginationProvider>(s => s.GetRequiredService<SnapshotPaginationProvider>());
            services.TryAddTransient<IAsyncSnapshotPaginationProvider>(s => s.GetRequiredService<SnapshotPaginationProvider>());
            services.TryAddTransient<ISnapshotPaginationCalculator, SnapshotPaginationCalculator>();
            services.TryAddTransient<SearchService>();   // searchListener
            services.TryAddTransient<IServiceListener>(s => s.GetRequiredService<SearchService>());
            services.TryAddTransient<IAsyncServiceListener>(s => s.GetRequiredService<SearchService>());
            services.TryAddTransient((provider) => new[]
            {
                provider.GetRequiredService<IServiceListener>()
            });
            services.TryAddTransient((provider) => new[]
            {
                provider.GetRequiredService<IAsyncServiceListener>()
            });
            services.TryAddTransient<ITransactionService, TransactionService>();            // transaction
            services.TryAddTransient<IAsyncTransactionService, AsyncTransactionService>();  // transaction
            services.TryAddTransient<HistoryService>();                    // history
            services.TryAddTransient<PagingService>(); // paging
            services.TryAddTransient<IPagingService>(s => s.GetRequiredService<PagingService>());
            services.TryAddTransient<IAsyncPagingService>(s => s.GetRequiredService<PagingService>());
            services.TryAddTransient<ResourceStorageService>();            // storage
            services.TryAddTransient<CapabilityStatementService>();        // conformance
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
                provider.GetRequiredService<IAsyncTransactionService>(),
                provider.GetRequiredService<HistoryService>(),
                provider.GetRequiredService<PagingService>(),
                provider.GetRequiredService<ResourceStorageService>(),
                provider.GetRequiredService<CapabilityStatementService>(),
                provider.GetRequiredService<PatchService>(),
            });

            services.TryAddSingleton((provider) => new FhirJsonParser(settings.ParserSettings));
            services.TryAddSingleton((provider) => new FhirXmlParser(settings.ParserSettings));
            services.TryAddSingleton((provder) => new FhirJsonSerializer(settings.SerializerSettings));
            services.TryAddSingleton((provder) => new FhirXmlSerializer(settings.SerializerSettings));

#pragma warning disable CS0618 // Type or member is obsolete
            services.TryAddSingleton<IFhirService, FhirService>();
#pragma warning restore CS0618 // Type or member is obsolete
            services.TryAddSingleton<IAsyncFhirService, AsyncFhirService>();

            IMvcCoreBuilder builder = services.AddFhirFormatters(settings, setupAction);

            services.RemoveAll<OutputFormatterSelector>();
            services.TryAddSingleton<OutputFormatterSelector, FhirOutputFormatterSelector>();

            services.RemoveAll<OutputFormatterSelector>();
            services.TryAddSingleton<OutputFormatterSelector, FhirOutputFormatterSelector>();

            return builder;
        }

        public static IMvcCoreBuilder AddFhirFormatters(this IServiceCollection services, SparkSettings settings, Action<MvcOptions> setupAction = null)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            return services.AddMvcCore(options =>
            {
                options.Filters.Add<UnsupportedMediaTypeFilter>(-3001);

                if (settings.UseAsynchronousIO)
                {
                    options.InputFormatters.Add(new AsyncResourceJsonInputFormatter(new FhirJsonParser(settings.ParserSettings)));
                    options.InputFormatters.Add(new AsyncResourceXmlInputFormatter(new FhirXmlParser(settings.ParserSettings)));
                    options.InputFormatters.Add(new BinaryInputFormatter());
                    options.OutputFormatters.Add(new AsyncResourceJsonOutputFormatter());
                    options.OutputFormatters.Add(new AsyncResourceXmlOutputFormatter());
                    options.OutputFormatters.Add(new BinaryOutputFormatter());
                }
                else
                {
                    options.InputFormatters.Add(new ResourceJsonInputFormatter(new FhirJsonParser(settings.ParserSettings), ArrayPool<char>.Shared));
                    options.InputFormatters.Add(new ResourceXmlInputFormatter(new FhirXmlParser(settings.ParserSettings)));
                    options.InputFormatters.Add(new BinaryInputFormatter());
                    options.OutputFormatters.Add(new ResourceJsonOutputFormatter());
                    options.OutputFormatters.Add(new ResourceXmlOutputFormatter());
                    options.OutputFormatters.Add(new BinaryOutputFormatter());
                }

                options.RespectBrowserAcceptHeader = true;

                setupAction?.Invoke(options);
            });
        }

        [Obsolete("This method is obsolete and will be removed in a future version.")]
        public static IMvcCoreBuilder AddFhirFormatters(this IServiceCollection services, Action<MvcOptions> setupAction = null)
        {
            return services.AddMvcCore(options =>
            {
                options.InputFormatters.Add(new ResourceJsonInputFormatter());
                options.InputFormatters.Add(new ResourceXmlInputFormatter());
                options.InputFormatters.Add(new BinaryInputFormatter());
                options.OutputFormatters.Add(new ResourceJsonOutputFormatter());
                options.OutputFormatters.Add(new ResourceXmlOutputFormatter());
                options.OutputFormatters.Add(new BinaryOutputFormatter());

                options.RespectBrowserAcceptHeader = true;

                setupAction?.Invoke(options);
            });
        }

        public static void AddCustomSearchParameters(this IServiceCollection services, IEnumerable<ModelInfo.SearchParamDefinition> searchParameters)
        {
            // Add any user-supplied SearchParameters
            ModelInfo.SearchParameters.AddRange(searchParameters);
        }

        private static void AddFhirHttpSearchParameters(this IServiceCollection services)
        {
            ModelInfo.SearchParameters.AddRange(new[]
            {
                new ModelInfo.SearchParamDefinition { Resource = "Resource", Name = "_id", Type = SearchParamType.String, Path = new string[] { "Resource.id" } }
                , new ModelInfo.SearchParamDefinition { Resource = "Resource", Name = "_lastUpdated", Type = SearchParamType.Date, Path = new string[] { "Resource.meta.lastUpdated" } }
                , new ModelInfo.SearchParamDefinition { Resource = "Resource", Name = "_tag", Type = SearchParamType.Token, Path = new string[] { "Resource.meta.tag" } }
                , new ModelInfo.SearchParamDefinition { Resource = "Resource", Name = "_profile", Type = SearchParamType.Uri, Path = new string[] { "Resource.meta.profile" } }
                , new ModelInfo.SearchParamDefinition { Resource = "Resource", Name = "_security", Type = SearchParamType.Token, Path = new string[] { "Resource.meta.security" } }
            });
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
}
#endif
