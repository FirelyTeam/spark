using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Service;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public class ConformanceService : IConformanceService
    {
        private readonly ILocalhost localhost;

        public ConformanceService(ILocalhost localhost)
        {
            this.localhost = localhost;
        }

        public Conformance GetSparkConformance(string sparkVersion)
        {
           return ConformanceBuilder.GetSparkConformance(sparkVersion, localhost);
        }

    }
}