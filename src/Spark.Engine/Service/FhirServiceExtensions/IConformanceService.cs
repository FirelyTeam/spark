using Hl7.Fhir.Model;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    internal interface IConformanceService : IFhirServiceExtension
    {
        Conformance GetSparkConformance(string sparkVersion);
    }
}