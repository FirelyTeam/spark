using System;
using System.Net.Http;
using Spark.Engine.Extensions;

namespace Spark.Engine.Core
{
    public class HistoryParameters
    {
        public HistoryParameters()
        {
            
        }
        public HistoryParameters(HttpRequestMessage request)
        {
            Count = request.GetIntParameter(FhirParameter.COUNT);
            Since = request.GetDateParameter(FhirParameter.SINCE);
            SortBy = request.GetParameter(FhirParameter.SORT);
        }

        public int? Count { get; set; }
        public DateTimeOffset? Since { get; set; }
        public string Format { get; set; }
        public string SortBy { get; set; }
    }
}