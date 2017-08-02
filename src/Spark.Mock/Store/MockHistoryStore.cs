using Spark.Engine.Store.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Store.Mock
{
	public class MockHistoryStore : IHistoryStore
	{
		public Snapshot History( string typename, HistoryParameters parameters )
		{
			throw new NotImplementedException();
		}

		public Snapshot History( IKey key, HistoryParameters parameters )
		{
			throw new NotImplementedException();
		}

		public Snapshot History( HistoryParameters parameters )
		{
			throw new NotImplementedException();
		}
	}
}
