using Hl7.Fhir.Model;

namespace Spark.Engine.Interfaces
{
    public interface IPagingExtension : IFhirStoreExtension
    {
        Bundle GetPage(string snapshotkey, int index);
    }
}