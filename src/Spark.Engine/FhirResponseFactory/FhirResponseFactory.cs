using System.Collections.Generic;
using System.Linq;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Interfaces;
using Spark.Service;

namespace Spark.Engine.FhirResponseFactory
{
    public class FhirResponseFactory : IFhirResponseFactory
    {
        protected IFhirStore fhirStore;
        protected Transfer transfer;
        private readonly IFhirResponseInterceptorRunner interceptorRunner;

        public FhirResponseFactory(IFhirStore fhirStore, Transfer transfer, IFhirResponseInterceptorRunner interceptorRunner)
        {
            this.fhirStore = fhirStore;
            this.transfer = transfer;
            this.interceptorRunner = interceptorRunner;
        }

        public FhirResponse GetFhirResponse(Key key, IEnumerable<object> parameters = null)
        {
            Interaction interaction = fhirStore.Get(key);

            if (interaction == null)
                return Respond.NotFound(key);
            return GetFhirResponse(interaction, parameters);
        }

        public FhirResponse GetFhirResponse(Interaction interaction, IEnumerable<object> parameters = null)
        {
            if (interaction.IsDeleted())
            {
                return Respond.Gone(interaction);
            }

            FhirResponse response = null;

            if (parameters != null)
            {
                response = interceptorRunner.RunInterceptors(interaction, parameters);
            }

            return response ?? Respond.WithResource(interaction);
        }

        public FhirResponse GetFhirResponse(Key key, params object[] parameters)
        {
            return GetFhirResponse(key, parameters.ToList());
        }

        public FhirResponse GetFhirResponse(Interaction interaction, params object[] parameters)
        {
            return GetFhirResponse(interaction, parameters.ToList());
        }
    }
}