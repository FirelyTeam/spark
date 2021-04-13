namespace Spark.Engine.Service.FhirServiceExtensions
{
    using Hl7.Fhir.Model;

    public interface IPatchService : IFhirServiceExtension
    {
        Resource Apply(Resource resource, Parameters patch);
    }
}