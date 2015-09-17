using System.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        //public class Tasks
        //{
        //    public const EventTask ServiceMethod = (EventTask)1;
        //}

        private static readonly Lazy<SparkMongoEventSource> Instance = new Lazy<SparkMongoEventSource>(() => new SparkMongoEventSource());

        private SparkMongoEventSource() { }

        public static SparkMongoEventSource Log { get { return Instance.Value; } }

        [Event(1, Message = "Method call: {0}",
            Level = EventLevel.Verbose, Keywords = Keywords.Tracing)]
        internal void ServiceMethodCalled(string methodName)
        {
            this.WriteEvent(1, methodName);
        }
    }
}
