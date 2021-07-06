using Spark.Engine.Core;
using Spark.Service;
using System.Threading.Tasks;

namespace Spark.Engine.Service
{
    public interface ICompositeServiceListener : IServiceListener
    {
        void Add(IServiceListener listener);
        void Clear();
        void Inform(Entry interaction);
        Task InformAsync(Entry interaction);
    }
}