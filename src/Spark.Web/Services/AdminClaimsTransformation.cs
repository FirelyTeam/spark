/*
 * Copyright (c) 2019-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;

namespace Spark.Web.Services;

public class AdminClaimsTransformation : IClaimsTransformation
{
    private readonly string[] _adminUsers;

    public AdminClaimsTransformation(IConfiguration configuration)
    {
        _adminUsers = configuration.GetSection("GitHub:AdminUsers").Get<string[]>() ?? [];
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity == null || !identity.IsAuthenticated)
        {
            return Task.FromResult(principal);
        }

        // Check if user is in admin allowlist (by username or email)
        var username = principal.FindFirstValue(ClaimTypes.Name);
        var email = principal.FindFirstValue(ClaimTypes.Email);

        var isAdmin = _adminUsers.Any(admin =>
            string.Equals(admin, username, System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(admin, email, System.StringComparison.OrdinalIgnoreCase));

        if (isAdmin && !principal.IsInRole("Admin"))
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
        }

        return Task.FromResult(principal);
    }
}
