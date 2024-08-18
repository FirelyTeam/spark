/* 
 * Copyright (c) 2020-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.Threading.Tasks;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface IIndexRebuildService
    {
        Task RebuildIndexAsync(IIndexBuildProgressReporter reporter = null);
    }
}
