/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

namespace Spark.Engine.Search.Support;

public interface IPositionInfo
{
    int LineNumber { get; }
    int LinePosition { get; }
}
