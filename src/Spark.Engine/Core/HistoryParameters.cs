using System;
using System.Net.Http;
using Spark.Engine.Extensions;
using Spark.Engine.Utility;
#if NETSTANDARD2_0
using Microsoft.AspNetCore.Http;
#endif

namespace Spark.Engine.Core
{
    public class HistoryParameters
    {
        public HistoryParameters()
        {
            
        }
        public HistoryParameters(HttpRequestMessage request)
        {
            Count = FhirParameterParser.ParseIntParameter(request.GetParameter(FhirParameter.COUNT));
            Since = FhirParameterParser.ParseDateParameter(request.GetParameter(FhirParameter.SINCE));
            SortBy = request.GetParameter(FhirParameter.SORT);
        }

#if NETSTANDARD2_0
        public HistoryParameters(HttpRequest request)
        {
            Count = FhirParameterParser.ParseIntParameter(request.GetParameter(FhirParameter.COUNT));
            Since = FhirParameterParser.ParseDateParameter(request.GetParameter(FhirParameter.SINCE));
            SortBy = request.GetParameter(FhirParameter.SORT);
        }
#endif

        public int? Count { get; set; }
        public DateTimeOffset? Since { get; set; }
        public string Format { get; set; }
        public string SortBy { get; set; }
    }
}