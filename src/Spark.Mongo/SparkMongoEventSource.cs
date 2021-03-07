using System.Diagnostics.Tracing;
using System;

namespace Spark.Mongo
{
    [EventSource(Name = "Furore-Spark-Mongo")]
    public sealed class SparkMongoEventSource : EventSource
    {
        public class Keywords
        {
            public const EventKeywords Tracing = (EventKeywords)1;
            public const EventKeywords Unsupported = (EventKeywords)2;
        }

        private static readonly Lazy<SparkMongoEventSource> _instance = new Lazy<SparkMongoEventSource>(() => new SparkMongoEventSource());

        private SparkMongoEventSource() { }

        public static SparkMongoEventSource Log { get { return _instance.Value; } }

        [Event(1, Message = "Method call: {0}",
            Level = EventLevel.Verbose, Keywords = Keywords.Tracing)]
        internal void ServiceMethodCalled(string methodName)
        {
            WriteEvent(1, methodName);
        }
    }
}
