using Spark.Engine.Store.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Store.Mock
{
	public class MockFhirStore : IFhirStore
	{
		Dictionary<IKey, Entry> Store = new Dictionary<IKey, Entry>();

		public void Add( Entry entry )
		{
			Store[entry.Key] = entry;
		}

		public Entry Get( IKey key )
		{
			if( Store.TryGetValue( key, out var entry ) )
				return entry;
			return null;
		}

		public IList<Entry> Get( IEnumerable<IKey> localIdentifiers )
		{
			var result = new List<Entry>();
			foreach( var key in localIdentifiers )
				if( Store.TryGetValue( key, out var entry ) )
					result.Add( entry );
			return result;
		}
	}
}