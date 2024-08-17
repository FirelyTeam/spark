/* 
 * Copyright (c) 2020-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Engine.Store.Interfaces
{
    public interface IFhirStorePagedReader
    {
        Task<IPageResult<Entry>> ReadAsync(FhirStorePageReaderOptions options = null);
    }

    public class FhirStorePageReaderOptions
    {
        public int PageSize { get; set; } = 100;

        // TODO: add criteria?
        // TODO: add sorting?
    }

    public interface IPageResult<out T>
    {
        long TotalRecords { get; }

        long TotalPages { get; }

        Task IterateAllPagesAsync(Func<IReadOnlyList<T>, Task> callback);
    }
}
