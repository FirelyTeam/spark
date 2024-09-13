/* 
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;

namespace Spark.Engine.Core;

public interface ILocalhost
{
    Uri DefaultBase { get; }
    Uri Absolute(Uri uri);
    bool IsBaseOf(Uri uri);
    Uri GetBaseOf(Uri uri);
}
