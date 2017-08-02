using Spark.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hl7.Fhir.Model;

namespace Spark.Store.Mock
{
	public class MockIdGenerator : IGenerator
	{
		Dictionary<string, int> NextID { get; } = new Dictionary<string, int>();

		public string NextResourceId( Resource resource ) => NextVersionId( resource.TypeName, resource.Id );

		public string NextVersionId( string resourceIdentifier )
		{
			if( NextID.TryGetValue( resourceIdentifier, out var id ) )
				NextID[resourceIdentifier] = ++id;
			else
				NextID[resourceIdentifier] = id = 1;
			return id.ToString();
		}

		public string NextVersionId( string resourceType, string resourceIdentifier ) => NextVersionId( resourceType + "/" + resourceIdentifier );
	}
}
