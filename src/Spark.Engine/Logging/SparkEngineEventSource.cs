using System.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Engine.Logging
{
    [EventSource(Name = "Furore-Spark-Engine")]
    public sealed class SparkEngineEventSource : EventSource
    {
        public class Keywords
        {
            public const EventKeywords ServiceMethod = (EventKeywords)1;
            public const EventKeywords Invalid = (EventKeywords)2;
            public const EventKeywords Unsupported = (EventKeywords)4;
            public const EventKeywords Tracing = (EventKeywords)8;
        }

        public class Tasks
        {
            public const EventTask ServiceMethod = (EventTask)1;
        }

        private static readonly Lazy<SparkEngineEventSource> Instance = new Lazy<SparkEngineEventSource>(() => new SparkEngineEventSource());

        private SparkEngineEventSource() { }

        public static SparkEngineEventSource Log { get { return Instance.Value; } }

        [Event(1, Message = "Service call: {0}",
            Level = EventLevel.Verbose, Keywords = Keywords.ServiceMethod)]
        internal void ServiceMethodCalled(string methodName)
        {
            this.WriteEvent(1, methodName);
        }

        [Event(2, Message = "Not supported: {0} in {1}",
         Level = EventLevel.Verbose, Keywords = Keywords.Unsupported)]
        internal void UnsupportedFeature(string methodName, string feature)
        {
            this.WriteEvent(2, feature, methodName);
        }

        [Event(4, Message = "Invalid Element",
         Level = EventLevel.Verbose, Keywords = Keywords.Unsupported)]
        internal void InvalidElement(string resourceID, string element, string message)
        {
            this.WriteEvent(4, message, resourceID, element);
        }
    }
}
