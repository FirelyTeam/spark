/* 
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions;

internal interface IHistoryService : IFhirServiceExtension
{
    Task<Snapshot> HistoryAsync(string typename, HistoryParameters parameters);
    Task<Snapshot> HistoryAsync(IKey key, HistoryParameters parameters);
    Task<Snapshot> HistoryAsync(HistoryParameters parameters);
}