/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;

namespace Spark.Auth.Controllers;

/// <summary>
/// Serves the SMART App Launch configuration document.
/// See: https://build.fhir.org/ig/HL7/smart-app-launch/conformance.html
/// </summary>
[ApiController]
public class SmartConfigurationController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public SmartConfigurationController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("~/.well-known/smart-configuration")]
    public IActionResult GetConfiguration()
    {
        var baseUrl = ResolvePublicBaseUrl();

        return Ok(new SmartConfigurationResponse
        {
            Issuer = baseUrl,
            AuthorizationEndpoint = $"{baseUrl}/connect/authorize",
            TokenEndpoint = $"{baseUrl}/connect/token",
            TokenEndpointAuthMethodsSupported = ["client_secret_post", "client_secret_basic"],
            GrantTypesSupported = ["authorization_code", "client_credentials"],
            ScopesSupported = ["openid", "email", "profile", "offline_access"],
            ResponseTypesSupported = ["code"],
            Capabilities = ["launch-standalone", "client-confidential-symmetric"],
            CodeChallengeMethodsSupported = ["S256"],
        });
    }

    private string ResolvePublicBaseUrl()
    {
        var endpoint = _configuration["SparkSettings:Endpoint"];
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri))
        {
            var basePath = endpointUri.AbsolutePath.TrimEnd('/');
            if (basePath.EndsWith("/fhir", StringComparison.OrdinalIgnoreCase))
            {
                basePath = basePath[..^5];
            }

            return $"{endpointUri.Scheme}://{endpointUri.Authority}{basePath}";
        }

        return $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
    }

    private sealed class SmartConfigurationResponse
    {
        [JsonPropertyName("issuer")]
        public string Issuer { get; init; } = string.Empty;

        [JsonPropertyName("authorization_endpoint")]
        public string AuthorizationEndpoint { get; init; } = string.Empty;

        [JsonPropertyName("token_endpoint")]
        public string TokenEndpoint { get; init; } = string.Empty;

        [JsonPropertyName("token_endpoint_auth_methods_supported")]
        public string[] TokenEndpointAuthMethodsSupported { get; init; } = [];

        [JsonPropertyName("grant_types_supported")]
        public string[] GrantTypesSupported { get; init; } = [];

        [JsonPropertyName("scopes_supported")]
        public string[] ScopesSupported { get; init; } = [];

        [JsonPropertyName("response_types_supported")]
        public string[] ResponseTypesSupported { get; init; } = [];

        [JsonPropertyName("capabilities")]
        public string[] Capabilities { get; init; } = [];

        [JsonPropertyName("code_challenge_methods_supported")]
        public string[] CodeChallengeMethodsSupported { get; init; } = [];
    }
}
