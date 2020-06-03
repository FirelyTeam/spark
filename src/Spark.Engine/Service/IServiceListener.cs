using System;
using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Service
{
    public interface IServiceListener
    {
        Task Inform(Uri location, Entry interaction);
    }
}
