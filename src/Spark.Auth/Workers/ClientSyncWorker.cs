/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Spark.Auth.Workers;

/// <summary>
/// Syncs client applications defined in SmartAuth:Clients with the store on startup.
/// Creates missing clients; existing clients are left unchanged.
/// </summary>
public class ClientSyncWorker : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SmartAuthSettings _settings;
    private readonly ILogger<ClientSyncWorker> _logger;

    public ClientSyncWorker(
        IServiceProvider serviceProvider,
        IOptions<SmartAuthSettings> settings,
        ILogger<ClientSyncWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    private static readonly HashSet<string> BuiltInScopes = ["openid", "email", "profile", "roles"];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_settings.Clients.Count == 0) return;

        await using var serviceScope = _serviceProvider.CreateAsyncScope();
        var manager = serviceScope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var scopeManager = serviceScope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

        // Register custom scopes that don't already exist
        var customScopes = _settings.Clients
            .SelectMany(c => c.Scopes)
            .Where(s => !BuiltInScopes.Contains(s))
            .Distinct();

        foreach (var scope in customScopes)
        {
            if (await scopeManager.FindByNameAsync(scope, cancellationToken) is null)
            {
                await scopeManager.CreateAsync(new OpenIddictScopeDescriptor { Name = scope }, cancellationToken);
            }
        }

        foreach (var client in _settings.Clients)
        {
            if (await manager.FindByClientIdAsync(client.ClientId, cancellationToken) is not null)
                continue;

            var redirectUris = new List<Uri>();
            var hasInvalidRedirectUri = false;
            foreach (var value in client.RedirectUris)
            {
                if (Uri.TryCreate(value, UriKind.Absolute, out var parsed))
                {
                    redirectUris.Add(parsed);
                    continue;
                }

                _logger.LogError("Skipping SmartAuth client {ClientId}: invalid RedirectUri '{RedirectUri}'.", client.ClientId, value);
                hasInvalidRedirectUri = true;
            }

            var postLogoutRedirectUris = new List<Uri>();
            var hasInvalidPostLogoutRedirectUri = false;
            foreach (var value in client.PostLogoutRedirectUris)
            {
                if (Uri.TryCreate(value, UriKind.Absolute, out var parsed))
                {
                    postLogoutRedirectUris.Add(parsed);
                    continue;
                }

                _logger.LogError("Skipping SmartAuth client {ClientId}: invalid PostLogoutRedirectUri '{PostLogoutRedirectUri}'.", client.ClientId, value);
                hasInvalidPostLogoutRedirectUri = true;
            }

            if (hasInvalidRedirectUri || hasInvalidPostLogoutRedirectUri)
                continue;

            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = client.ClientId,
                ClientSecret = client.ClientSecret,
                DisplayName = client.DisplayName ?? client.ClientId,
                ConsentType = ConsentTypes.Explicit,
            };

            foreach (var uri in redirectUris)
                descriptor.RedirectUris.Add(uri);

            foreach (var uri in postLogoutRedirectUris)
                descriptor.PostLogoutRedirectUris.Add(uri);

            // Standard endpoint permissions
            descriptor.Permissions.Add(Permissions.Endpoints.Authorization);
            descriptor.Permissions.Add(Permissions.Endpoints.Token);
            descriptor.Permissions.Add(Permissions.Endpoints.EndSession);
            descriptor.Permissions.Add(Permissions.ResponseTypes.Code);

            // Grant type permissions
            foreach (var grantType in client.GrantTypes)
            {
                var permission = grantType switch
                {
                    "authorization_code" => Permissions.GrantTypes.AuthorizationCode,
                    "client_credentials" => Permissions.GrantTypes.ClientCredentials,
                    "refresh_token" => Permissions.GrantTypes.RefreshToken,
                    _ => Permissions.Prefixes.GrantType + grantType,
                };
                descriptor.Permissions.Add(permission);
            }

            // Scope permissions
            foreach (var scope in client.Scopes)
            {
                var permission = scope switch
                {
                    "email" => Permissions.Scopes.Email,
                    "profile" => Permissions.Scopes.Profile,
                    "roles" => Permissions.Scopes.Roles,
                    _ => Permissions.Prefixes.Scope + scope,
                };
                descriptor.Permissions.Add(permission);
            }

            if (client.RequirePkce)
                descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);

            await manager.CreateAsync(descriptor, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
