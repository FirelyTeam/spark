using Spark.Engine.Core;

namespace Spark.Engine.Store.Interfaces
{
    public interface IFhirStoreExtension : IExtension<IFhirStore>
    {
        void OnEntryAdded(Entry entry);
      
    }
}