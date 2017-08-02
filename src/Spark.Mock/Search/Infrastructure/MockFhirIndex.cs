using Spark.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;

namespace Spark.Mock.Search.Infrastructure
{
	public class MockFhirIndex : IFhirIndex
	{
		public void Clean()
		{
			throw new NotImplementedException();
		}

		public Key FindSingle( string resource, SearchParams searchCommand )
		{
			throw new NotImplementedException();
		}

		public SearchResults GetReverseIncludes( IList<IKey> keys, IList<string> revIncludes )
		{
			throw new NotImplementedException();
		}

		public void Process( IEnumerable<Entry> entries )
		{
			throw new NotImplementedException();
		}

		public void Process( Entry entry )
		{
			throw new NotImplementedException();
		}

		public SearchResults Search( string resource, SearchParams searchCommand )
		{
			throw new NotImplementedException();
		}
	}
}
