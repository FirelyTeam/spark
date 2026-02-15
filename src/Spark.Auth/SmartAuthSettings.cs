/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Collections.Generic;

namespace Spark.Auth;

public class SmartAuthSettings
{
    public bool Enabled { get; set; }
    public List<ClientDefinition> Clients { get; set; } = [];
}

public class ClientDefinition
{
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public List<string> RedirectUris { get; set; } = [];
    public List<string> PostLogoutRedirectUris { get; set; } = [];
    public List<string> Scopes { get; set; } = [];
    public List<string> GrantTypes { get; set; } = [];
    public bool RequirePkce { get; set; } = true;
}
