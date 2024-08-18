/* 
 * Copyright (c) 2020-2024, Incendi (info@incendi.no)
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

#if NETSTANDARD2_0 || NET6_0
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Spark.Engine.Maintenance
{
    public class MaintenanceModeHandler
    {
        private readonly RequestDelegate _next;

        public MaintenanceModeHandler(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (MaintenanceMode.IsEnabledForHttpMethod(context.Request.Method))
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                return;
            }
            await _next(context);
        }
    }
}
#endif