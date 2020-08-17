using Spark.Engine.Core;
using System.Net;

namespace Spark.Engine.Maintenance
{
    internal class MaintenanceModeEnabledException : SparkException
    {
        public MaintenanceModeEnabledException() : base(HttpStatusCode.ServiceUnavailable)
        {
        }
    }
}
