using System;
using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Service
{
    public interface IAsyncServiceListener
    {
        Task InformAsync(Uri location, Entry interaction);
    }
}
