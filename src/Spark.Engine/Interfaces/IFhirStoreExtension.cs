using Spark.Engine.Core;

namespace Spark.Engine.Interfaces
{
    public interface IFhirStoreExtension : IExtension<IBaseFhirStore>
    {
        void OnEntryAdded(Entry entry);
      
    }
}