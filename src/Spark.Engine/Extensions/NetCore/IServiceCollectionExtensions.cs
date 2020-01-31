#if NETSTANDARD2_0
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Spark.Engine.Core;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Formatters;
using Spark.Engine.Interfaces;
using Spark.Engine.Search;
using Spark.Engine.Service;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Formatters;
using Spark.Service;
using System;
using System.Collections.Generic;
using System.Net.Http.Formatting;

namespace Spark.Engine.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IMvcCoreBuilder AddFhir(this IServiceCollection services, SparkSettings settings, Action<MvcOptions> setupAction = null)
        {
            services.AddFhirHttpSearchParameters();

            services.TryAddSingleton<SparkSettings>(settings);
            services.TryAddTransient<ElementIndexer>();
            services.TryAddTransient<IIndexService, IndexService>();
            services.TryAddTransient<ILocalhost>((provider) => new Localhost(settings.Endpoint));
            services.TryAddTransient<IFhirModel>((provider) => new FhirModel(ModelInfo.SearchParameters));
            services.TryAddTransient((provider) => new FhirPropertyIndex(provider.GetRequiredService<IFhirModel>()));
            services.TryAddTransient<ITransfer, Transfer>();
            services.TryAddTransient<ConditionalHeaderFhirResponseInterceptor>();
            services.TryAddTransient((provider) => new IFhirResponseInterceptor[] { provider.GetRequiredService<ConditionalHeaderFhirResponseInterceptor>() });
            services.TryAddTransient<IFhirResponseInterceptorRunner, FhirResponseInterceptorRunner>();
            services.TryAddTransient<IFhirResponseFactory, FhirResponseFactory.FhirResponseFactory>();
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
            });

            services.TryAddSingleton((provider) => new FhirJsonParser(settings.ParserSettings));
            services.TryAddSingleton((provider) => new FhirXmlParser(settings.ParserSettings));
            services.TryAddSingleton((provder) => new FhirJsonSerializer(settings.SerializerSettings));
            services.TryAddSingleton((provder) => new FhirXmlSerializer(settings.SerializerSettings));

            services.TryAddSingleton<IFhirService, FhirService>();

            IMvcCoreBuilder builder = services.AddFhirFormatters(setupAction);

            services.RemoveAll<OutputFormatterSelector>();
            services.TryAddSingleton<OutputFormatterSelector, FhirOutputFormatterSelector>();

            return builder;
        }

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
    }
}
#endif