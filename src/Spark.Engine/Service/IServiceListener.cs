using System;
using Spark.Engine.Core;

namespace Spark.Service
{
    using System.Threading.Tasks;

    public interface IServiceListener
    {
        Task Inform(Uri location, Entry interaction);
    }

}
