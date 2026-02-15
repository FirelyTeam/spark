/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Spark.Auth.Controllers;

[ApiController]
public class AuthorizationController : ControllerBase
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IAuthenticationSchemeProvider _authenticationSchemeProvider;

    public AuthorizationController(
        IOpenIddictApplicationManager applicationManager,
        IAuthenticationSchemeProvider authenticationSchemeProvider)
    {
        _applicationManager = applicationManager;
        _authenticationSchemeProvider = authenticationSchemeProvider;
    }

    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // Try to retrieve the user principal stored in the authentication cookie.
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!result.Succeeded)
        {
            return await ChallengeInteractiveLoginAsync();
        }

        var identity = CreateUserIdentity(result.Principal!, request.GetScopes());

        return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/token")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsClientCredentialsGrantType())
        {
            var application = await _applicationManager.FindByClientIdAsync(request.ClientId!)
                ?? throw new InvalidOperationException("The application details cannot be found.");

            var identity = new ClaimsIdentity(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                Claims.Name, Claims.Role);

            identity.SetClaim(Claims.Subject, await _applicationManager.GetClientIdAsync(application));
            identity.SetClaim(Claims.Name, await _applicationManager.GetDisplayNameAsync(application));

            identity.SetScopes(request.GetScopes());
            identity.SetDestinations(static claim => [Destinations.AccessToken]);

            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            var principal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal
                ?? throw new InvalidOperationException("The authorization code or refresh token is no longer valid.");

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new InvalidOperationException("The specified grant type is not supported.");
    }

    [HttpGet("~/connect/endsession")]
    [HttpPost("~/connect/endsession")]
    public async Task<IActionResult> EndSession()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return SignOut(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties { RedirectUri = "/" });
    }

    private async Task<IActionResult> ChallengeInteractiveLoginAsync()
    {
        var challengeScheme = await _authenticationSchemeProvider.GetDefaultChallengeSchemeAsync();
        if (challengeScheme is null || string.Equals(challengeScheme.Name, CookieAuthenticationDefaults.AuthenticationScheme, StringComparison.Ordinal))
        {
            return BadRequest(new
            {
                error = Errors.InvalidRequest,
                error_description = "Interactive login is not configured for the authorization endpoint. Configure GitHub OAuth or use client_credentials."
            });
        }

        return Challenge(
            authenticationSchemes: challengeScheme.Name,
            properties: new AuthenticationProperties
            {
                RedirectUri = Request.PathBase + Request.Path + QueryString.Create(
                    Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList())
            });
    }

    private static ClaimsIdentity CreateUserIdentity(ClaimsPrincipal principal, IEnumerable<string> scopes)
    {
        var claims = new List<Claim>
        {
            new(Claims.Subject, principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue(ClaimTypes.Name)
                ?? throw new InvalidOperationException("The user identifier cannot be found.")),
            new(Claims.Name, principal.FindFirstValue(ClaimTypes.Name) ?? ""),
        };

        var email = principal.FindFirstValue(ClaimTypes.Email);
        if (!string.IsNullOrEmpty(email))
        {
            claims.Add(new Claim(Claims.Email, email));
        }

        var identity = new ClaimsIdentity(claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        identity.SetScopes(scopes);
        identity.SetDestinations(GetClaimDestinations);
        return identity;
    }

    private static IEnumerable<string> GetClaimDestinations(Claim claim)
    {
        // Include identity claims in id_token only when corresponding OIDC scopes were granted.
        if (claim.Type == Claims.Name && claim.Subject?.HasScope(Scopes.Profile) is true)
            return [Destinations.AccessToken, Destinations.IdentityToken];

        if (claim.Type == Claims.Email && claim.Subject?.HasScope(Scopes.Email) is true)
            return [Destinations.AccessToken, Destinations.IdentityToken];

        return [Destinations.AccessToken];
    }
}
