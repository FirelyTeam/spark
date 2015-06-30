using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core.Service
{
    // Is not used yet.

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
