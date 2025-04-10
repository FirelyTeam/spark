/* 
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.AspNetCore.Mvc.Filters;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using System.Linq;

namespace Spark.Engine.Filters;

internal class UnsupportedMediaTypeFilter : IActionFilter
{
    ///<inheritdoc/>
    public void OnActionExecuted(ActionExecutedContext context)
    {
            
    }

    ///<inheritdoc/>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var request = context.HttpContext.Request;

        if (request.IsRawBinaryRequest()) return;

        if (request.Headers.ContainsKey("Accept"))
        {
            var acceptHeader = request.Headers["Accept"].ToString();
            if (!FhirMediaType.SupportedMimeTypes.Any(mimeType => acceptHeader.Contains(mimeType)))
            {
                throw Error.NotAcceptable();
            }
        }

        if (context.HttpContext.Request.ContentType != null)
        {
            if (!FhirMediaType.SupportedMimeTypes.Any(mimeType => context.HttpContext.Request.ContentType.Contains(mimeType)))
            {
                throw Error.UnsupportedMediaType();
            }
        }
    }
}
