#if NETSTANDARD2_0
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spark.Engine.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Engine.Formatters
{
    internal class FhirOutputFormatterSelector : DefaultOutputFormatterSelector
    {
        private IOptions<MvcOptions> _options;
        public FhirOutputFormatterSelector(IOptions<MvcOptions> options, ILoggerFactory loggerFactory) : base(options, loggerFactory)
        {
            _options = options;
        }

        public override IOutputFormatter SelectFormatter(OutputFormatterCanWriteContext context, IList<IOutputFormatter> formatters, MediaTypeCollection contentTypes)
        {
            if(context.IsRawBinaryRequest(context.ObjectType))
            {
                IOutputFormatter formatter = formatters.Where(f => f is BinaryOutputFormatter).SingleOrDefault();
                if (formatter != null) return formatter;
                formatter = _options.Value.OutputFormatters.Where(f => f is BinaryOutputFormatter).SingleOrDefault();
                if (formatter != null) return formatter;
            }

            return base.SelectFormatter(context, formatters, contentTypes);
        }
    }
}
#endif