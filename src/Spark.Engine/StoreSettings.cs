/*
 * Copyright (c) 2019-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Engine.Store;

namespace Spark.Engine;

public class StoreSettings
{
    public string ConnectionString { get; set; }
    public IndexQueueSettings IndexQueue { get; set; } = new();
}
