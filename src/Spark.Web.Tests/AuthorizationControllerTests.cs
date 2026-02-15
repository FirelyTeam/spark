/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using Spark.Auth.Controllers;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Spark.Web.Tests;

public class AuthorizationControllerTests
{
    [Fact]
    public async Task Authorize_ReturnsInvalidRequest_WhenNoInteractiveChallengeSchemeIsConfigured()
    {
        var services = CreateServices(configureAuthentication: options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        });

        var controller = CreateController(services);

        var result = await controller.Authorize();

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var json = JsonSerializer.Serialize(badRequest.Value);
        Assert.Contains(Errors.InvalidRequest, json, StringComparison.Ordinal);
        Assert.Contains("Interactive login is not configured", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Authorize_ChallengesDefaultExternalScheme_WhenInteractiveProviderIsConfigured()
    {
        var services = CreateServices(configureAuthentication: options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = "GitHub";
        }, addInteractiveScheme: true);

        var controller = CreateController(services);

        var result = await controller.Authorize();

        var challenge = Assert.IsType<ChallengeResult>(result);
        Assert.Contains("GitHub", challenge.AuthenticationSchemes);
    }

    private static AuthorizationController CreateController(ServiceProvider services)
    {
        var appManager = new Mock<IOpenIddictApplicationManager>(MockBehavior.Strict);
        var schemeProvider = services.GetRequiredService<IAuthenticationSchemeProvider>();

        var controller = new AuthorizationController(appManager.Object, schemeProvider)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext(services)
            }
        };

        return controller;
    }

    private static DefaultHttpContext CreateHttpContext(ServiceProvider services)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = services
        };

        context.Request.Path = "/connect/authorize";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost", 5001);

        var transaction = new OpenIddictServerTransaction
        {
            Request = new OpenIddictRequest
            {
                ClientId = "smart-app",
                ResponseType = ResponseTypes.Code,
                Scope = Scopes.OpenId
            }
        };

        context.Features.Set(new OpenIddictServerAspNetCoreFeature
        {
            Transaction = transaction
        });

        return context;
    }

    private static ServiceProvider CreateServices(Action<AuthenticationOptions> configureAuthentication, bool addInteractiveScheme = false)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();

        var builder = serviceCollection
            .AddAuthentication(configureAuthentication)
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);

        if (addInteractiveScheme)
        {
            builder.AddScheme<AuthenticationSchemeOptions, NoResultAuthHandler>("GitHub", _ => { });
        }

        return serviceCollection.BuildServiceProvider();
    }

    private sealed class NoResultAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public NoResultAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            System.Text.Encodings.Web.UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            => Task.FromResult(AuthenticateResult.NoResult());
    }
}
