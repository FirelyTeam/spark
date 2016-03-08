using Spark.Engine.Core;

namespace Spark.Engine.Interfaces
{
    public interface IFhirStoreExtension
    {
        void OnEntryAdded(Entry entry);
        void OnExtensionAdded(IBaseFhirStore fhirStore);
    }
}