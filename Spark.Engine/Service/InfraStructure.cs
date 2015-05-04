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

}
