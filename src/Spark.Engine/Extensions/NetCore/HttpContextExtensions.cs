#if NETSTANDARD2_0
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Spark.Engine.Extensions
{
    public static class HttpContextExtensions
    {
        public static IOutputFormatter SelectFormatter(this HttpContext context, OutputFormatterWriteContext writeContext)
        {
            var outputFormatterSelector = context.RequestServices.GetRequiredService<OutputFormatterSelector>();
            return outputFormatterSelector.SelectFormatter(writeContext, Array.Empty<IOutputFormatter>(), new MediaTypeCollection());
        }

        public static OutputFormatterWriteContext GetOutputFormatterWriteContext<T>(this HttpContext context, T model)
        {
            var writerFactory = context.RequestServices.GetRequiredService<IHttpResponseStreamWriterFactory>();
            return new OutputFormatterWriteContext(context, writerFactory.CreateWriter, typeof(T), model);
        }
    }
}
#endif