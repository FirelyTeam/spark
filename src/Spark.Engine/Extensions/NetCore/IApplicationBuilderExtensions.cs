#if NETSTANDARD2_0
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using System;

namespace Spark.Engine.Extensions
{
    public static class IApplicationBuilderExtensions
    {
        public static void UseFhir(this IApplicationBuilder app, Action<IRouteBuilder> configureRoutes)
        {
            app.UseMvc(configureRoutes);
        }
    }
}
#endif