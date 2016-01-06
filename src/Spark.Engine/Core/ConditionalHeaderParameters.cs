using System;
using System.Collections.Generic;
using System.Net.Http;
using Spark.Engine.Extensions;

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

        public IEnumerable<string> IfNoneMatchTags { get; set; }
        public DateTimeOffset? IfModifiedSince { get; set; }
    }
}