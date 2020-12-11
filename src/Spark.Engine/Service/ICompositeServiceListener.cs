using Spark.Engine.Core;
using Spark.Service;

namespace Spark.Engine.Service
{
    using System.Threading.Tasks;

    public interface ICompositeServiceListener : IServiceListener
    {
        void Add(IServiceListener listener);
        void Clear();
        Task Inform(Entry interaction);
    }
}