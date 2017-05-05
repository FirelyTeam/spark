using Spark.Engine.Store.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spark.Engine.Core;
using Spark.Engine.Model;

namespace Spark.Mock.Search.Infrastructure
{
	public class MockIndexStore : IIndexStore
	{
		public void Clean()
		{
			throw new NotImplementedException();
		}

		public void Delete( Entry entry )
		{
			throw new NotImplementedException();
		}

		public void Save( IndexValue indexValue )
		{
			throw new NotImplementedException();
		}
	}
}
