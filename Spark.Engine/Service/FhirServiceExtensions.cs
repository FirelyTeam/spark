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
        public IFhirIndex Index { get; set; }
        public IServiceListener ServiceListener { get; set; }

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

   

}
