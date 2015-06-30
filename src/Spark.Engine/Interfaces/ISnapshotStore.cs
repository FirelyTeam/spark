using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Core
{
    public interface ISnapshotStore
    {
        void AddSnapshot(Snapshot snapshot);
        Snapshot GetSnapshot(string snapshotid);
    }
}
