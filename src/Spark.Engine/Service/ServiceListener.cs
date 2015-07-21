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

        private void Inform(IServiceListener listener, Uri location, Interaction interaction)
        {
            listener.Inform(location, interaction);
        }

        public void Clear()
        {
            listeners.Clear();
        }

        public void Inform(Uri location, Interaction interaction)
        {
            foreach(IServiceListener listener in listeners)
            {
                Inform(listener, location, interaction);
            }
        }
    }
}
