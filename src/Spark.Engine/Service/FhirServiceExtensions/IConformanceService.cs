using Hl7.Fhir.Model;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    internal interface ICapabilityStatementService : IFhirServiceExtension
    {
        CapabilityStatement GetSparkCapabilityStatement(string sparkVersion);
    }
}