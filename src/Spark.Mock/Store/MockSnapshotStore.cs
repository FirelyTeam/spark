using Spark.Engine.Store.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Store.Mock
{
	public class MockSnapshotStore : ISnapshotStore
	{
		public void AddSnapshot( Snapshot snapshot )
		{
			throw new NotImplementedException();
		}

		public Snapshot GetSnapshot( string snapshotid )
		{
			throw new NotImplementedException();
		}
	}
}
