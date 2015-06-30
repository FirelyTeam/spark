using Spark.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Service
{
    public interface IServiceListener
    {
        void Inform(Uri location, Interaction interaction);
    }

}
