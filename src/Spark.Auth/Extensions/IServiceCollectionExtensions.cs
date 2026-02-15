/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Spark.Auth.Workers;

namespace Spark.Auth.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddSparkAuth(this IServiceCollection services, SmartAuthSettings settings, string mongoConnectionString)
    {
        services.Configure<SmartAuthSettings>(options =>
        {
            options.Enabled = settings.Enabled;
            options.Clients = settings.Clients;
        });

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseMongoDb()
                    .UseDatabase(new MongoClient(mongoConnectionString)
                        .GetDatabase(MongoUrl.Create(mongoConnectionString).DatabaseName));
            })
            .AddServer(options =>
            {
                options
                    .SetAuthorizationEndpointUris("connect/authorize")
                    .SetTokenEndpointUris("connect/token")
                    .SetEndSessionEndpointUris("connect/endsession");

                options
                    .AllowAuthorizationCodeFlow()
                    .AllowClientCredentialsFlow()
                    .AllowRefreshTokenFlow();

                // Register signing and encryption credentials for development.
                // In production, use .AddSigningCertificate() and .AddEncryptionCertificate().
                options
                    .AddDevelopmentEncryptionCertificate()
                    .AddDevelopmentSigningCertificate();

                options
                    .UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        services.AddHostedService<ClientSyncWorker>();

        return services;
    }
}
