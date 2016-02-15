using System;
using System.Collections.Generic;
using Spark.Engine.Core;

namespace Spark.Service
{

    public class ServiceListener : IServiceListener
    {
        List<IServiceListener> listeners;

        public ServiceListener()
        {
            listeners = new List<IServiceListener>();
        }

        public void Add(IServiceListener listener)
        {
            this.listeners.Add(listener);
        }

        private void Inform(IServiceListener listener, Uri location, Entry entry)
        {
            listener.Inform(location, entry);
        }

        public void Clear()
        {
            listeners.Clear();
        }

        public void Inform(Uri location, Entry entry)
        {
            foreach(IServiceListener listener in listeners)
            {
                Inform(listener, location, entry);
            }
        }
    }
}
