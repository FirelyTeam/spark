using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Mock
{
	public sealed class MockEventSource : EventSource
	{
		public class Keywords
		{
			public const EventKeywords Tracing = (EventKeywords)1;
			public const EventKeywords Unsupported = (EventKeywords)2;
		}

		private static readonly Lazy<MockEventSource> Instance = new Lazy<MockEventSource>( () => new MockEventSource() );

		private MockEventSource() { }

		public static MockEventSource Log { get { return Instance.Value; } }

		[Event( 1, Message = "Method call: {0}",
			Level = EventLevel.Verbose, Keywords = Keywords.Tracing )]
		internal void ServiceMethodCalled( string methodName )
		{
			this.WriteEvent( 1, methodName );
		}
	}
}