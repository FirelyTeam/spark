using System;
using Spark.Engine.Core;

namespace Spark.Service
{
    public interface IServiceListener
    {
        void Inform(Uri location, Interaction interaction);
    }

}
