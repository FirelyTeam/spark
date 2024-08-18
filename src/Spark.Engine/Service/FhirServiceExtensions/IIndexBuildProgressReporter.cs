/* 
 * Copyright (c) 2020-2024, Incendi (info@incendi.no)
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.Threading.Tasks;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface IIndexBuildProgressReporter
    {
        Task ReportProgressAsync(int progress, string message);

        Task ReportErrorAsync(string message);
    }
}