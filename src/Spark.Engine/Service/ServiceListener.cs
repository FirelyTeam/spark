using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Spark.Engine.Core;
using Spark.Engine.Service;

namespace Spark.Service
{
    public class ServiceListener : IServiceListener, IAsyncServiceListener, ICompositeServiceListener
    {
        private readonly ILocalhost _localhost;
        readonly List<IServiceListener> _listeners;
        readonly List<IAsyncServiceListener> _asyncListeners;

        public ServiceListener(ILocalhost localhost, 
            IServiceListener[] listeners = null, 
            IAsyncServiceListener[] asyncListeners = null)
        {
            _localhost = localhost;
            if(listeners != null)
                _listeners = new List<IServiceListener>(listeners.AsEnumerable());
            if (asyncListeners != null)
                _asyncListeners = asyncListeners.ToList();
        }

        public void Add(IServiceListener listener)
        {
            _listeners.Add(listener);
        }

        private void Inform(IServiceListener listener, Uri location, Entry entry)
        {
            listener.Inform(location, entry);
        }

        private async Task InformAsync(IAsyncServiceListener listener, Uri location, Entry entry)
        {
            await listener.InformAsync(location, entry).ConfigureAwait(false);
        }

        public void Clear()
        {
            _listeners.Clear();
        }

        public void Inform(Entry interaction)
        {
            foreach (IServiceListener listener in _listeners)
            {
                Uri location = _localhost.GetAbsoluteUri(interaction.Key);
                Inform(listener, location, interaction);
            }
        }

        public async Task InformAsync(Entry interaction)
        {
            foreach (var listener in _asyncListeners)
            {
                Uri location = _localhost.GetAbsoluteUri(interaction.Key);
                await InformAsync(listener, location, interaction).ConfigureAwait(false);
            }
        }

        public void Inform(Uri location, Entry entry)
        {
            foreach (IServiceListener listener in _listeners)
            {
                Inform(listener, location, entry);
            }
        }

        public async Task InformAsync(Uri location, Entry interaction)
        {
            foreach (var listener in _asyncListeners)
            {
                await listener.InformAsync(location, interaction).ConfigureAwait(false);
            }
        }
    }
}
