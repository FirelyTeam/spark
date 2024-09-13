/* 
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions;

public interface ISnapshotPagination
{
    Task<Bundle> GetPageAsync(int? index = null, Action<Entry> transformElement = null);
}