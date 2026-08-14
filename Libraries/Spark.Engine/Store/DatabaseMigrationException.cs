/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;

namespace Spark.Engine.Store;

public sealed class DatabaseMigrationException : Exception
{
    public DatabaseMigrationException(string message) : base(message)
    {
    }

    public DatabaseMigrationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
