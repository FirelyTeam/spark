﻿/* 
 * Copyright (c) 2016-2018, Furore (info@furore.com) and contributors
 * Copyright (c) 2021-2023, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    internal interface IHistoryService : IFhirServiceExtension
    {
        Task<Snapshot> HistoryAsync(string typename, HistoryParameters parameters);
        Task<Snapshot> HistoryAsync(IKey key, HistoryParameters parameters);
        Task<Snapshot> HistoryAsync(HistoryParameters parameters);
    }
}