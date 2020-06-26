#if NETSTANDARD2_0
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Spark.Engine.Extensions
{
    public static class HttpContextExtensions
    {
        private const string RESOURCE_TYPE_KEY = "resourceType";

        public static IOutputFormatter SelectFormatter(this HttpContext context, OutputFormatterWriteContext writeContext)
        {
            var outputFormatterSelector = context.RequestServices.GetRequiredService<OutputFormatterSelector>();
            return outputFormatterSelector.SelectFormatter(writeContext, Array.Empty<IOutputFormatter>(), new MediaTypeCollection());
        }

        public static OutputFormatterWriteContext GetOutputFormatterWriteContext<T>(this HttpContext context, T model)
        {
            return context.GetOutputFormatterWriteContext(typeof(T), model);
        }

        public static OutputFormatterWriteContext GetOutputFormatterWriteContext(this HttpContext context, Type type, object model)
        {
            var writerFactory = context.RequestServices.GetRequiredService<IHttpResponseStreamWriterFactory>();
            return new OutputFormatterWriteContext(context, writerFactory.CreateWriter, type, model);
        }

        public static void AllowSynchronousIO(this HttpContext context)
        {
            var bodyControlFeature = context.Features.Get<IHttpBodyControlFeature>();
            if (bodyControlFeature != null)
            {
                bodyControlFeature.AllowSynchronousIO = true;
            }
        }

        public static void AddResourceType(this HttpContext context, Type resourceType)
        {
            context.Items.Add(RESOURCE_TYPE_KEY, resourceType);
        }

        public static Type GetResourceType(this HttpContext context)
        {
            return context.Items[RESOURCE_TYPE_KEY] as Type;
        }
    }
}
#endif