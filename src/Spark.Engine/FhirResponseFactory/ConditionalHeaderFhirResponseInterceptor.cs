/* 
 * Copyright (c) 2015-2018, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.Linq;
using System.Net;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Interfaces;

namespace Spark.Engine.FhirResponseFactory
{
    public class ConditionalHeaderFhirResponseInterceptor : IFhirResponseInterceptor
    {
        public bool CanHandle(object input)
        {
            return input is ConditionalHeaderParameters;
        }

        private ConditionalHeaderParameters ConvertInput(object input)
        {
            return input as ConditionalHeaderParameters;
        }

        public FhirResponse GetFhirResponse(Entry entry, object input)
        {
            ConditionalHeaderParameters parameters = ConvertInput(input);
            if (parameters == null) return null;

            bool? matchTags = parameters.IfNoneMatchTags.Any() ? parameters.IfNoneMatchTags.Any(t => t == ETag.Create(entry.Key.VersionId).Tag) : (bool?)null;
            bool? matchModifiedDate = parameters.IfModifiedSince.HasValue
                ? parameters.IfModifiedSince.Value < entry.Resource.Meta.LastUpdated
                : (bool?) null;

            if (!matchTags.HasValue  && !matchModifiedDate.HasValue)
            {
                return null;
            }

            if ((matchTags ?? true) && (matchModifiedDate ?? true))
            {
                return Respond.WithCode(HttpStatusCode.NotModified);
            }

            return null;
        }
    }
}