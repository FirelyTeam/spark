using System;
using System.Collections.Generic;
using System.Linq;
using Spark.Engine.Core;
using Spark.Engine.Service;

namespace Spark.Service
{
    using System.Threading.Tasks;

    public class ServiceListener : IServiceListener, ICompositeServiceListener
    {
        private readonly ILocalhost localhost;
        private readonly List<IServiceListener> listeners;

        public ServiceListener(ILocalhost localhost, IServiceListener[] listeners = null)
        {
            this.localhost = localhost;
            if (listeners != null)
                this.listeners = new List<IServiceListener>(listeners.AsEnumerable());
        }

        public void Add(IServiceListener listener)
        {
            this.listeners.Add(listener);
        }

        private Task Inform(IServiceListener listener, Uri location, Entry entry)
        {
            return listener.Inform(location, entry);
        }

        public void Clear()
        {
            listeners.Clear();
        }

        public Task Inform(Entry interaction)
        {
            // todo: what we want is not to send localhost to the listener, but to add the Resource.Base. But that is not an option in the current infrastructure.
            // It would modify interaction.Resource, while 

            return Task.WhenAll(
                listeners.Select(listener => Inform(listener, localhost.GetAbsoluteUri(interaction.Key), interaction)));
        }

        public Task Inform(Uri location, Entry entry)
        {
            return Task.WhenAll(
                listeners.Select(listener => Inform(listener, location, entry)));
        }
    }
}
