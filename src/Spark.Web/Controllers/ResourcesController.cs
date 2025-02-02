/*
 * Copyright (c) 2019-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.AspNetCore.Mvc;

namespace Spark.Web.Controllers;

public class ResourcesController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}