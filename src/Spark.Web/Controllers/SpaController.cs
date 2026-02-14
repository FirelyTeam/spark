/*
 * Copyright (c) 2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Spark.Web.Controllers;

/// <summary>
/// Fallback controller for SPA client-side routing.
/// Serves index.html for routes not handled by other controllers.
/// </summary>
public class SpaController : Controller
{
    private readonly IWebHostEnvironment _env;

    public SpaController(IWebHostEnvironment env)
    {
        _env = env;
    }

    public IActionResult Index()
    {
        return PhysicalFile(
            System.IO.Path.Combine(_env.WebRootPath, "index.html"),
            "text/html");
    }
}
