using Hl7.Fhir.Model;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface ICapabilityStatementService : IFhirServiceExtension
    {
        CapabilityStatement GetSparkCapabilityStatement(string sparkVersion);
    }
}