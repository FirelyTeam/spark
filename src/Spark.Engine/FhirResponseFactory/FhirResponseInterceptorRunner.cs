using System.Collections.Generic;
using System.Linq;
using Spark.Engine.Core;
using Spark.Engine.Interfaces;

namespace Spark.Engine.FhirResponseFactory
{
    public class FhirResponseInterceptorRunner : IFhirResponseInterceptorRunner
    {
        private readonly IList<IFhirResponseInterceptor> interceptors;

        public FhirResponseInterceptorRunner(IFhirResponseInterceptor[] interceptors)
        {
            this.interceptors = new List<IFhirResponseInterceptor>(interceptors);
        }

        public void AddInterceptor(IFhirResponseInterceptor interceptor)
        {
            interceptors.Add(interceptor);
        }

        public void ClearInterceptors()
        {
            interceptors.Clear();
        }

        public FhirResponse RunInterceptors(Entry entry, IEnumerable<object> parameters)
        {
            FhirResponse response = null;
            parameters.FirstOrDefault(p => (response = RunInterceptors(entry, (object) p)) != null);
            return response;
        }

        private FhirResponse RunInterceptors(Entry entry, object input)
        {
            FhirResponse response = null;
            GetResponseInterceptors(input).FirstOrDefault(f => (response = f.GetFhirResponse(entry, input)) != null);
            return response;
        }
        private IEnumerable<IFhirResponseInterceptor> GetResponseInterceptors(object input)
        {
            return interceptors.Where(i => i.CanHandle(input));
        }
    }
}