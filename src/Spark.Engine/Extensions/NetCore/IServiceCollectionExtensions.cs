﻿#if NETSTANDARD2_0
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Spark.Engine.Core;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Filters;
using Spark.Engine.Formatters;
using Spark.Engine.Interfaces;
using Spark.Engine.Search;
using Spark.Engine.Service;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Service;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Engine.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IMvcCoreBuilder AddFhir(this IServiceCollection services, SparkSettings settings, Action<MvcOptions> setupAction = null)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            services.AddFhirHttpSearchParameters();

            services.SetContentTypeAsFhirMediaTypeOnValidationError();

            services.TryAddSingleton<SparkSettings>(settings);
            services.TryAddTransient<ElementIndexer>();

            services.TryAddTransient<IReferenceNormalizationService, ReferenceNormalizationService>();

            services.TryAddTransient<IIndexService, IndexService>();
            services.TryAddTransient<ILocalhost>((provider) => new Localhost(settings.Endpoint));
            services.TryAddTransient<IFhirModel>((provider) => new FhirModel(ModelInfo.SearchParameters));
            services.TryAddTransient((provider) => new FhirPropertyIndex(provider.GetRequiredService<IFhirModel>()));
            services.TryAddTransient<ITransfer, Transfer>();
            services.TryAddTransient<ConditionalHeaderFhirResponseInterceptor>();
            services.TryAddTransient((provider) => new IFhirResponseInterceptor[] { provider.GetRequiredService<ConditionalHeaderFhirResponseInterceptor>() });
            services.TryAddTransient<IFhirResponseInterceptorRunner, FhirResponseInterceptorRunner>();
            services.TryAddTransient<IFhirResponseFactory, FhirResponseFactory.FhirResponseFactory>();
            services.TryAddTransient<IIndexRebuildService, IndexRebuildService>();
            services.TryAddTransient<ISearchService, SearchService>();
            services.TryAddTransient<ISnapshotPaginationProvider, SnapshotPaginationProvider>();
            services.TryAddTransient<ISnapshotPaginationCalculator, SnapshotPaginationCalculator>();
            services.TryAddTransient<IServiceListener, SearchService>();   // searchListener
            services.TryAddTransient((provider) => new IServiceListener[] { provider.GetRequiredService<IServiceListener>() });
            services.TryAddTransient<SearchService>();                     // search
            services.TryAddTransient<TransactionService>();                // transaction
            services.TryAddTransient<HistoryService>();                    // history
            services.TryAddTransient<PagingService>();                     // paging
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
                provider.GetRequiredService<TransactionService>(),
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