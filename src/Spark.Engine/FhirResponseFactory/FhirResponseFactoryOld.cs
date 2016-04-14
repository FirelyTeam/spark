using System.Collections.Generic;
using System.Linq;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Interfaces;
using Spark.Service;

namespace Spark.Engine.FhirResponseFactory
{
    public class FhirResponseFactoryOld : IFhirResponseFactoryOld
    {
        protected IFhirStoreFull FhirStoreFull;
        protected Transfer transfer;
        private readonly IFhirResponseInterceptorRunner interceptorRunner;

        public FhirResponseFactoryOld(IFhirStoreFull FhirStoreFull, Transfer transfer, IFhirResponseInterceptorRunner interceptorRunner)
        {
            this.FhirStoreFull = FhirStoreFull;
            this.transfer = transfer;
            this.interceptorRunner = interceptorRunner;
        }

        public FhirResponse GetFhirResponse(Key key, IEnumerable<object> parameters = null)
        {
            Entry entry = FhirStoreFull.Get(key);

            if (entry == null)
                return Respond.NotFound(key);
            return GetFhirResponse(entry, parameters);
        }

        public FhirResponse GetFhirResponse(Entry entry, IEnumerable<object> parameters = null)
        {
            if (entry.IsDeleted())
            {
                return Respond.Gone(entry);
            }

            FhirResponse response = null;

            if (parameters != null)
            {
                response = interceptorRunner.RunInterceptors(entry, parameters);
            }

            return response ?? Respond.WithResource(entry);
        }

        public FhirResponse GetFhirResponse(Key key, params object[] parameters)
        {
            return GetFhirResponse(key, parameters.ToList());
        }

        public FhirResponse GetFhirResponse(Entry entry, params object[] parameters)
        {
            return GetFhirResponse(entry, parameters.ToList());
        }

      
    }
}