using Spark.Engine.Core;
using Spark.Service;
using System;
using System.Threading.Tasks;

namespace Spark.Engine.Service
{
    public interface ICompositeServiceListener : IServiceListener
    {
        void Add(IServiceListener listener);
        void Clear();
        [Obsolete("Use ICompositeServiceListener.InformAsync instead.")]
        void Inform(Entry interaction);

        Task InformAsync(Entry interaction);
    }
}