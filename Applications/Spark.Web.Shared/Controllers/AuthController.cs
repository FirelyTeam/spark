/*
 * Copyright (c) 2019-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Spark.Web.Controllers;

[Route("api/auth"), ApiController]
public class AuthController : ControllerBase
{
    private readonly bool _gitHubEnabled;

    public AuthController(IConfiguration configuration)
    {
        var clientId = configuration["GitHub:ClientId"];
        var clientSecret = configuration["GitHub:ClientSecret"];
        _gitHubEnabled = !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret);
    }

    [HttpGet("status")]
    public ActionResult<AuthStatusResponse> GetStatus()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Ok(new AuthStatusResponse
            {
                IsAuthenticated = false,
                AuthEnabled = _gitHubEnabled,
                Username = null,
                Email = null,
                AvatarUrl = null,
                Roles = []
            });
        }

        return Ok(new AuthStatusResponse
        {
            IsAuthenticated = true,
            AuthEnabled = _gitHubEnabled,
            Username = User.FindFirstValue(ClaimTypes.Name),
            Email = User.FindFirstValue(ClaimTypes.Email),
            AvatarUrl = User.FindFirstValue("urn:github:avatar"),
            Roles = User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList()
        });
    }

    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? returnUrl = "/admin")
    {
        if (!_gitHubEnabled)
        {
            return BadRequest(new { error = "GitHub OAuth is not configured. Set GitHub:ClientId and GitHub:ClientSecret in appsettings.json" });
        }

        // Validate returnUrl to prevent open redirect attacks
        var relativeReturnUrl = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : "/admin";

        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect(relativeReturnUrl);
        }

        return Challenge(new AuthenticationProperties
        {
            RedirectUri = relativeReturnUrl
        }, "GitHub");
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { message = "Logged out successfully" });
    }
}

public class AuthStatusResponse
{
    public bool IsAuthenticated { get; set; }
    public bool AuthEnabled { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public List<string> Roles { get; set; } = [];
}
