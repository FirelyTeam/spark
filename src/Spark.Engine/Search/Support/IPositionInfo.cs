/* 
 * Copyright (c) 2015-2018, Firely (info@fire.ly) and contributors
 * Copyright (c) 2021-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

namespace Spark.Search.Support
{
    public interface IPostitionInfo
    {
        int LineNumber { get; }
        int LinePosition { get; }
    }
}
