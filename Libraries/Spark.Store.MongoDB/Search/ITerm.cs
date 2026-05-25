/*
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Store.MongoDB.Search.Common;

namespace Spark.Store.MongoDB.Search;

public interface ITerm
{
    string Resource { get; set; }
    string Field { get; set; }
    string Operator { get; set; }
    string Value { get; set; }
    Argument Argument { get; set; }
}
