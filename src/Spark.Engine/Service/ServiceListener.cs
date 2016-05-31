using System;
using System.Collections.Generic;
using System.Linq;
using Spark.Engine.Core;
using Spark.Engine.Service;

namespace Spark.Service
{

    public class ServiceListener : IServiceListener, ICompositeServiceListener
    {
        private readonly ILocalhost localhost;
        readonly List<IServiceListener> listeners;

        public ServiceListener(ILocalhost localhost, IServiceListener[] listeners = null)
        {
            this.localhost = localhost;
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

        public void Inform(Entry interaction)
        {
            // todo: what we want is not to send localhost to the listener, but to add the Resource.Base. But that is not an option in the current infrastructure.
            // It would modify interaction.Resource, while 
            foreach (IServiceListener listener in listeners)
            {
                Uri location = localhost.GetAbsoluteUri(interaction.Key);
                Inform(listener, location, interaction);
            }
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
