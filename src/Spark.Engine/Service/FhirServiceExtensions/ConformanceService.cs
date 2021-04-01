using Hl7.Fhir.Model;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public class CapabilityStatementService : ICapabilityStatementService
    {
        private readonly ILocalhost _localhost;

        public CapabilityStatementService(ILocalhost localhost)
        {
            _localhost = localhost;
        }

        public CapabilityStatement GetSparkCapabilityStatement(string sparkVersion)
        {
           return CapabilityStatementBuilder.GetSparkCapabilityStatement(sparkVersion, _localhost);
        }
    }
}