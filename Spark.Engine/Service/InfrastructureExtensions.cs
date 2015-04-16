using Spark.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core
{
    public static class BasicInfrastructureExtensions
    {

        public static Infrastructure AddLocalhost(this Infrastructure infrastructure, ILocalhost localhost)
        {
            infrastructure.Localhost = localhost;
            return infrastructure;
        }

        public static Infrastructure AddLocalhost(this Infrastructure infrastructure, Uri url)
        {
            return infrastructure.AddLocalhost(new Localhost(url));
        }

        public static FhirService CreateService(this Infrastructure infrastructure)
        {
            return new FhirService(infrastructure);
        }

    }
}
