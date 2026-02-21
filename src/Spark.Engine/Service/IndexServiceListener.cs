/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Engine.Service;

/// <summary>
/// An <see cref="IServiceListener"/> that processes FHIR resource write events
/// synchronously via <see cref="IIndexService"/>.
/// </summary>
public class IndexServiceListener : IServiceListener
{
    private readonly IIndexService _indexService;

    public IndexServiceListener(IIndexService indexService)
    {
        _indexService = indexService;
    }

    public Task InformAsync(Uri location, Entry interaction)
    {
        return _indexService.ProcessAsync(interaction);
    }
}
