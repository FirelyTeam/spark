using System.Runtime.InteropServices.ComTypes;

namespace Spark.Engine.Interfaces
{
    public interface IExtendableFhirStore
    {
        void AddExtension<T>(T extension) where T : IFhirStoreExtension;
        void RemoveExtension<T>() where T : IFhirStoreExtension;
        T FindExtension<T>() where T : IFhirStoreExtension;
    }
}