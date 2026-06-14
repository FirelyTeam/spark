/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;

namespace Spark.Search.Mongo;

internal sealed class UnknownSearchParameterException : ArgumentException
{
    public UnknownSearchParameterException(string message) : base(message)
    {
    }
}
