/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Spark.Engine.Core;
using Spark.Engine.Service.FhirServiceExtensions;
using System.Collections.Generic;

namespace Spark.Engine.Extensions;

public static class IServiceCollectionExtensions
{
    public static IMvcBuilder AddFhir(this IServiceCollection services, SparkSettings settings, System.Action<MvcOptions> setupAction = null)
    {
        services.TryAddSingleton<IFhirModel>(_ => new FhirModel(ModelInfo.SearchParameters));
        services.TryAddSingleton<ICapabilityStatementService, CapabilityStatementService>();

        var builder = services.AddFhirInternal(settings, setupAction);

        services.AddTransient((provider) => new IFhirServiceExtension[]
        {
            provider.GetRequiredService<SearchService>(),
            provider.GetRequiredService<ITransactionService>(),
            provider.GetRequiredService<HistoryService>(),
            provider.GetRequiredService<PagingService>(),
            provider.GetRequiredService<ResourceStorageService>(),
            provider.GetRequiredService<ICapabilityStatementService>(),
            provider.GetRequiredService<PatchService>(),
        });

        return builder;
    }

    public static void AddCustomSearchParameters(this IServiceCollection services, IEnumerable<SearchParamDefinition> searchParameters)
    {
        ModelInfo.SearchParameters.AddRange(searchParameters);
    }
}
