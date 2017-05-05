using Spark.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hl7.Fhir.Model;

namespace Spark.Store.Mock
{
	public class MockGenerator : IGenerator
	{
		public string NextResourceId( Resource resource )
		{
			throw new NotImplementedException();
		}

		public string NextVersionId( string resourceIdentifier )
		{
			throw new NotImplementedException();
		}

		public string NextVersionId( string resourceType, string resourceIdentifier )
		{
			throw new NotImplementedException();
		}
	}
}
