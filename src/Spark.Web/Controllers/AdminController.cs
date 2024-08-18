/*
 * Copyright (c) 2019-2024, Incendi (info@incendi.no)
 * See the file CONTRIBUTORS for details.
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Spark.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Maintenance()
        {
            return View();
        }
    }
}