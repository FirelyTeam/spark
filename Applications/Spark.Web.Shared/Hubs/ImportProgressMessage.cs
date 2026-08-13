/*
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

namespace Spark.Web.Hubs;

internal class ImportProgressMessage
{
    public int Progress { get; set; }

    public string Message { get; set; }
}
