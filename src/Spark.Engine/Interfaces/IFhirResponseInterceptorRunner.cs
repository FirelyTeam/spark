using System.Collections.Generic;
using Spark.Engine.Core;
using Spark.Engine.Service;

namespace Spark.Engine.Interfaces
{
    public interface IFhirResponseInterceptorRunner
    {
        void AddInterceptor(IFhirResponseInterceptor interceptor);
        void ClearInterceptors();
        FhirResponse RunInterceptors(Entry entry, IEnumerable<object> parameters);
    }
}