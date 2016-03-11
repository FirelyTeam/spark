using Spark.Engine.Core;

namespace Spark.Engine.Interfaces
{
    public interface IFhirStoreExtension : IExtension<IFhirStore>
    {
        void OnEntryAdded(Entry entry);
      
    }
}