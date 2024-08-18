/* 
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.Diagnostics.Tracing;
using System;

namespace Spark.Engine.Logging
{
    [EventSource(Name = "Spark-Engine")]
    public sealed class SparkEngineEventSource : EventSource
    {
        private static readonly Lazy<SparkEngineEventSource> _instance = new Lazy<SparkEngineEventSource>(() => new SparkEngineEventSource());

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

        private SparkEngineEventSource() { }

        public static SparkEngineEventSource Log { get { return _instance.Value; } }

        [Event(1, Message = "Service call: {0}",
            Level = EventLevel.Verbose, Keywords = Keywords.ServiceMethod)]
        internal void ServiceMethodCalled(string methodName)
        {
            WriteEvent(1, methodName);
        }

        [Event(2, Message = "Not supported: {0} in {1}",
         Level = EventLevel.Verbose, Keywords = Keywords.Unsupported)]
        internal void UnsupportedFeature(string methodName, string feature)
        {
            WriteEvent(2, feature, methodName);
        }

        [Event(4, Message = "Invalid Element",
         Level = EventLevel.Verbose, Keywords = Keywords.Unsupported)]
        internal void InvalidElement(string resourceID, string element, string message)
        {
            WriteEvent(4, message, resourceID, element);
        }
    }
}
