using System.Threading.Tasks;
using Spark.Engine.Core;
using Spark.Service;

namespace Spark.Engine.Service
{
    public interface ICompositeServiceListener : IServiceListener
    {
        void Add(IServiceListener listener);
        void Clear();
        Task Inform(Entry interaction);
    }
}
