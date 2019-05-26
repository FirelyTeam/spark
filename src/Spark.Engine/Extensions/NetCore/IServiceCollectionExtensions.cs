#if NETSTANDARD2_0
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Spark.Engine.Core;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Formatters;
using Spark.Engine.Interfaces;
using Spark.Engine.Model;
using Spark.Engine.Service;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Service;
using System;

namespace Spark.Engine.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IMvcCoreBuilder AddFhir(this IServiceCollection services, SparkSettings settings = null, Action<MvcOptions> setupAction = null)
        {
            if (settings == null) settings = new SparkSettings { ParserSettings = new ParserSettings { PermissiveParsing = true } };
            AddFhirHttpSearchParameters();

            services.AddTransient<ILocalhost, Localhost>((provider) => new Localhost(settings.Endpoint));
            services.AddTransient<IFhirModel, FhirModel>((provider) => new FhirModel(SparkModelInfo.SparkSearchParameters));
            services.AddTransient((provider) => new FhirPropertyIndex(provider.GetRequiredService<IFhirModel>()));
            services.AddTransient<ITransfer, Transfer>();
            services.AddTransient<ConditionalHeaderFhirResponseInterceptor>();
            services.AddTransient((provider) => new IFhirResponseInterceptor[] { provider.GetRequiredService<ConditionalHeaderFhirResponseInterceptor>() });
            services.AddTransient<IFhirResponseInterceptorRunner, FhirResponseInterceptorRunner>();
            services.AddTransient<IFhirResponseFactory, FhirResponseFactory.FhirResponseFactory>();
            services.AddTransient<ISearchService, SearchService>();
            services.AddTransient<ISnapshotPaginationProvider, SnapshotPaginationProvider>();
            services.AddTransient<ISnapshotPaginationCalculator, SnapshotPaginationCalculator>();
            services.AddTransient<IServiceListener, SearchService>();   // searchListener
            services.AddTransient((provider) => new IServiceListener[] { provider.GetRequiredService<IServiceListener>() });
            services.AddTransient<SearchService>();                     // search
            services.AddTransient<TransactionService>();                // transaction
            services.AddTransient<HistoryService>();                    // history
            services.AddTransient<PagingService>();                     // paging
            services.AddTransient<ResourceStorageService>();            // storage
            services.AddTransient<ConformanceService>();                // conformance
            services.AddTransient<ICompositeServiceListener, ServiceListener>();
            services.AddTransient<ResourceJsonInputFormatter>();
            services.AddTransient<ResourceJsonOutputFormatter>();
            services.AddTransient<ResourceXmlInputFormatter>();
            services.AddTransient<ResourceXmlOutputFormatter>();

            services.AddTransient((provider) => new IFhirServiceExtension[] 
            {
                provider.GetRequiredService<SearchService>(),
                provider.GetRequiredService<TransactionService>(),
                provider.GetRequiredService<HistoryService>(),
                provider.GetRequiredService<PagingService>(),
                provider.GetRequiredService<ResourceStorageService>(),
                provider.GetRequiredService<ConformanceService>(),
            });

            services.AddSingleton((provider) => new FhirJsonParser(settings.ParserSettings));
            services.AddSingleton((provider) => new FhirXmlParser(settings.ParserSettings));
            services.AddSingleton((provder) => new FhirJsonSerializer(settings.SerializerSettings));
            services.AddSingleton((provder) => new FhirXmlSerializer(settings.SerializerSettings));

            services.AddSingleton<IFhirService, FhirService>();

            IMvcCoreBuilder builder = services.AddMvcCore(options =>
            {
                options.InputFormatters.Clear();
                options.OutputFormatters.Clear();

                options.InputFormatters.Add(new ResourceJsonInputFormatter());
                options.InputFormatters.Add(new ResourceXmlInputFormatter());
                options.InputFormatters.Add(new BinaryInputFormatter());

                options.OutputFormatters.Add(new ResourceJsonOutputFormatter());
                options.OutputFormatters.Add(new ResourceXmlOutputFormatter());
                options.OutputFormatters.Add(new BinaryOutputFormatter());

                options.RespectBrowserAcceptHeader = true;

                setupAction?.Invoke(options);
            });

            return builder;
        }

        public static void AddFhirHttpSearchParameters()
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