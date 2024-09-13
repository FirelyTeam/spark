/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2019-2024, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Spark.Engine.Core;
using Spark.Engine.Service;

namespace Spark.Service;

public class ServiceListener : IServiceListener, ICompositeServiceListener
{
    private readonly ILocalhost _localhost;
    readonly List<IServiceListener> _listeners;

    public ServiceListener(ILocalhost localhost, IServiceListener[] listeners = null)
    {
        _localhost = localhost;
        if(listeners != null)
            _listeners = new List<IServiceListener>(listeners.AsEnumerable());
    }

    public void Add(IServiceListener listener)
    {
        _listeners.Add(listener);
    }

    private async Task InformAsync(IServiceListener listener, Uri location, Entry entry)
    {
        await listener.InformAsync(location, entry).ConfigureAwait(false);
    }

    public void Clear()
    {
        _listeners.Clear();
    }

    public async Task InformAsync(Entry interaction)
    {
        foreach (IServiceListener listener in _listeners)
        {
            Uri location = _localhost.GetAbsoluteUri(interaction.Key);
            await InformAsync(listener, location, interaction).ConfigureAwait(false);
        }
    }

    public async Task InformAsync(Uri location, Entry interaction)
    {
        foreach (var listener in _listeners)
        {
            await listener.InformAsync(location, interaction).ConfigureAwait(false);
        }
    }
}