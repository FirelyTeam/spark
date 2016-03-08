using Hl7.Fhir.Model;
using Spark.Engine.Core;

namespace Spark.Engine.Interfaces
{
    public interface IPagingExtension : IFhirStoreExtension
    {
        Bundle GetPage(string snapshotkey, int index);
    }
}