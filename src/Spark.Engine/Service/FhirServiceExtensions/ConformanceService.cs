using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Service;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public class CapabilityStatementService : ICapabilityStatementService
    {
        private readonly ILocalhost localhost;

        public CapabilityStatementService(ILocalhost localhost)
        {
            this.localhost = localhost;
        }

        public CapabilityStatement GetSparkCapabilityStatement(string sparkVersion)
        {
           return CapabilityStatementBuilder.GetSparkCapabilityStatement(sparkVersion, localhost);
        }

    }
}