using System;
using System.Collections.Generic;
using System.Linq;
using Spark.Engine.Core;
using Spark.Engine.Service;

namespace Spark.Service
{

    public class ServiceListener : IServiceListener, ICompositeServiceListener
    {
        readonly List<IServiceListener> listeners;

        public ServiceListener(IServiceListener[] listeners = null)
        {
            this.listeners = new List<IServiceListener>(listeners.AsEnumerable());
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
