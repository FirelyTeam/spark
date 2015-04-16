using Spark.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spark.Core;

namespace Spark.Core
{

    public class Infrastructure
    {
        public ILocalhost Localhost { get; set; }
        public IFhirStore Store { get; set; }
        public IGenerator Generator { get; set; }
        public ISnapshotStore SnapshotStore { get; set; }

    }

    public static class InfrastructureProvider
    {
        private volatile static Dictionary<string, Infrastructure> items = new Dictionary<string, Infrastructure>();

        public static Infrastructure Create(string name)
        {
            var infrastructure = new Infrastructure();
            items.Add(name, infrastructure);
            return infrastructure;
        }

        public static Infrastructure Get(string name)
        {
            Infrastructure infrastructure;
            if (items.TryGetValue(name, out infrastructure))
            {
                return infrastructure;
            }
            else
            {
                return null;
            }
        }

    }

    public static class InfrastructureExtensions
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
