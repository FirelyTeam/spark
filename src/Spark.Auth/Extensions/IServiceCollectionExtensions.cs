/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using OpenIddict.Server;
using Spark.Auth.Workers;

namespace Spark.Auth.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddSparkAuth(
        this IServiceCollection services,
        SmartAuthSettings settings,
        string mongoConnectionString,
        bool useDevelopmentCertificates)
    {
        services.Configure<SmartAuthSettings>(options =>
        {
            options.Enabled = settings.Enabled;
            options.Clients = settings.Clients;
            options.Endpoints = settings.Endpoints;
        });

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseMongoDb()
                    .UseDatabase(new MongoClient(mongoConnectionString)
                        .GetDatabase(MongoUrl.Create(mongoConnectionString).DatabaseName));
            })
            .AddServer(options => ConfigureOpenIdServer(options, settings, useDevelopmentCertificates))
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        services.AddHostedService<ClientSyncWorker>();

        return services;
    }

    private static void ConfigureOpenIdServer(OpenIddictServerBuilder options, SmartAuthSettings settings, bool useDevelopmentCertificates)
    {
        var endpointSettings = settings.Endpoints ?? new SmartAuthEndpointSettings();

        options
            .SetAuthorizationEndpointUris(NormalizeAndValidateEndpointPath(
                endpointSettings.AuthorizationEndpointPath,
                "SmartAuth:Endpoints:AuthorizationEndpointPath"))
            .SetTokenEndpointUris(NormalizeAndValidateEndpointPath(
                endpointSettings.TokenEndpointPath,
                "SmartAuth:Endpoints:TokenEndpointPath"))
            .SetEndSessionEndpointUris(NormalizeAndValidateEndpointPath(
                endpointSettings.EndSessionEndpointPath,
                "SmartAuth:Endpoints:EndSessionEndpointPath"))
            .AllowAuthorizationCodeFlow()
            .AllowClientCredentialsFlow()
            .AllowRefreshTokenFlow();

        ConfigureTokenCertificates(options, settings, useDevelopmentCertificates);

        options
            .UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableTokenEndpointPassthrough()
            .EnableEndSessionEndpointPassthrough();
    }

    private static void ConfigureTokenCertificates(OpenIddictServerBuilder options, SmartAuthSettings settings, bool useDevelopmentCertificates)
    {
        if (useDevelopmentCertificates)
        {
            options
                .AddDevelopmentEncryptionCertificate()
                .AddDevelopmentSigningCertificate();

            return;
        }

        var signingCertificate = LoadCertificate(
            settings.Certificates.SigningCertificatePath,
            settings.Certificates.SigningCertificatePassword,
            "SmartAuth:Certificates:SigningCertificatePath");

        var encryptionCertificate = LoadCertificate(
            settings.Certificates.EncryptionCertificatePath,
            settings.Certificates.EncryptionCertificatePassword,
            "SmartAuth:Certificates:EncryptionCertificatePath");

        options
            .AddSigningCertificate(signingCertificate)
            .AddEncryptionCertificate(encryptionCertificate);
    }

    private static X509Certificate2 LoadCertificate(string path, string password, string settingName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException($"Missing required configuration value: {settingName}.");
        }

        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Certificate file not found at configured path '{path}' ({settingName}).");
        }

        return X509CertificateLoader.LoadPkcs12FromFile(path, password, X509KeyStorageFlags.EphemeralKeySet);
    }

    private static string NormalizeAndValidateEndpointPath(string configuredPath, string settingName)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            throw new InvalidOperationException($"Invalid endpoint path in {settingName}. Value cannot be empty or whitespace.");
        }

        return configuredPath.Trim().TrimStart('/');
    }
}
