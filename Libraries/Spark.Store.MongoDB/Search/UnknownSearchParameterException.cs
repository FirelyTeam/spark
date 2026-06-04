/*
 * Copyright (c) 2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;

namespace Spark.Store.MongoDB.Search;

/// <summary>
/// Thrown when a resource does not declare the requested search parameter. Derives from
/// <see cref="ArgumentException"/> for backward compatibility, but lets chained-search resolution
/// tell a genuinely unknown parameter apart from other (real) query failures that must surface.
/// </summary>
internal sealed class UnknownSearchParameterException : ArgumentException
{
    public UnknownSearchParameterException(string message) : base(message) { }
}
