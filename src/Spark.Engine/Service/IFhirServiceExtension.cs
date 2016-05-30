using System.Dynamic;
using Spark.Engine.Store.Interfaces;

namespace Spark.Engine.Service
{
    public interface IFhirServiceExtension
    {
        bool EnableForStore(IStorageBuilder builder);
    }
}