using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Engine.Interfaces;

namespace Spark.Engine.Store.Interfaces
{
    public interface IPagingExtension : IFhirStoreExtension
    {
        Bundle GetPage(string snapshotkey, int index);
        Bundle GetPage(Snapshot snapshot, int index);
    }
}