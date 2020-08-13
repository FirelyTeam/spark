﻿#if NETSTANDARD2_0
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Spark.Engine.ExceptionHandling;
using Spark.Engine.Handlers.NetCore;
using System;
using Spark.Engine.Maintenance;

namespace Spark.Engine.Extensions
{
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
}
#endif