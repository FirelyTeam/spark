/* 
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Mongo.Search.Common;

namespace Spark.Search.Mongo
{
    public interface ITerm
    {
        string Resource { get; set; }
        string Field { get; set; }
        string Operator { get; set; }
        string Value { get; set; }
        Argument Argument { get; set; }
    }
}