using System;
using System.Collections.Generic;
using System.Net.Http;
using Spark.Engine.Extensions;
#if NETSTANDARD2_0
using Microsoft.AspNetCore.Http;
#endif

namespace Spark.Engine.Core
{
    public class ConditionalHeaderParameters
    {
        public ConditionalHeaderParameters()
        {
            
        }
        public ConditionalHeaderParameters(HttpRequestMessage request)
        {
            IfNoneMatchTags = request.IfNoneMatch();
            IfModifiedSince = request.IfModifiedSince();
        }

#if NETSTANDARD2_0
        public ConditionalHeaderParameters(HttpRequest request)
        {
            IfNoneMatchTags = request.IfNoneMatch();
            IfModifiedSince = request.IfModifiedSince();
        }
#endif

        public IEnumerable<string> IfNoneMatchTags { get; set; }
        public DateTimeOffset? IfModifiedSince { get; set; }
    }
}