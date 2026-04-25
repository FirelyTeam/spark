/* 
 * Copyright (c) 2019-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spark.Engine.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Engine.Formatters;

internal class FhirOutputFormatterSelector : DefaultOutputFormatterSelector
{
    private readonly IOptions<MvcOptions> _options;

    public FhirOutputFormatterSelector(IOptions<MvcOptions> options, ILoggerFactory loggerFactory) : base(options, loggerFactory)
    {
        _options = options;
    }

    public override IOutputFormatter SelectFormatter(OutputFormatterCanWriteContext context, IList<IOutputFormatter> formatters, MediaTypeCollection contentTypes)
    {
        if (!context.IsRawBinaryRequest(context.ObjectType))
            return base.SelectFormatter(context, formatters, contentTypes);

        IOutputFormatter formatter = formatters.SingleOrDefault(f => f is BinaryOutputFormatter);
        if (formatter != null)
            return formatter;
        formatter = _options.Value.OutputFormatters.SingleOrDefault(f => f is BinaryOutputFormatter);

        return formatter ?? base.SelectFormatter(context, formatters, contentTypes);
    }
}
