/* 
 * Copyright (c) 2019-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Spark.Engine.ExceptionHandling;
using Spark.Engine.Handlers;
using System;
using Spark.Engine.Maintenance;

namespace Spark.Engine.Extensions;

public static class IApplicationBuilderExtensions
{
    public static void UseFhir(this IApplicationBuilder app, Action<IRouteBuilder> configureRoutes = null)
    {
        app.UseMiddleware<ErrorHandler>();
        app.UseMiddleware<FormatTypeHandler>();
        app.UseMiddleware<MaintenanceModeHandler>();

        if (configureRoutes == null)
            app.UseMvc();
        else
            app.UseMvc(configureRoutes);
    }
}
