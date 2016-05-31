using Spark.Engine.Core;
using Spark.Service;

namespace Spark.Engine.Service
{
    public interface ICompositeServiceListener : IServiceListener
    {
        void Add(IServiceListener listener);
        void Clear();
        void Inform(Entry interaction);
    }
}