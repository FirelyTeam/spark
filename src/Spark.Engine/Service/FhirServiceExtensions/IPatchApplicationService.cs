namespace Spark.Engine.Service.FhirServiceExtensions
{
    using Hl7.Fhir.Model;

    public interface IPatchApplicationService : IFhirServiceExtension
    {
        Resource Apply(Resource resource, Parameters patch);
    }
}