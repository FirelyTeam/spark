/*
 * Copyright (c) 2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.Threading.Tasks;

namespace Spark.Web.Hubs;

/// <summary>
/// Strongly-typed interface for MaintenanceHub client methods.
/// Provides type safety for SignalR client communications.
/// </summary>
public interface IMaintenanceHub
{
    /// <summary>
    /// Sends a progress update message to connected clients.
    /// </summary>
    /// <param name="message">The progress message or ImportProgressMessage object</param>
    Task UpdateProgress(object message);
}

