/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

namespace Spark.Engine;

/// <summary>
/// Controls how the search index is updated after a FHIR resource write.
/// </summary>
public enum IndexingMode
{
    /// <summary>
    /// Index updates are processed synchronously in the HTTP request path via
    /// <c>IndexServiceListener</c>. Default behavior.
    /// </summary>
    Synchronous,

    /// <summary>
    /// Index updates are enqueued to the durable <c>indexqueue</c> MongoDB collection
    /// and processed by <c>IndexWorker</c> running as a background service.
    /// Search results become eventually consistent.
    /// </summary>
    Background
}
