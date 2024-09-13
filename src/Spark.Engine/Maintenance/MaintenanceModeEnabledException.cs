/* 
 * Copyright (c) 2020-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Engine.Core;
using System.Net;

namespace Spark.Engine.Maintenance;

internal class MaintenanceModeEnabledException : SparkException
{
    public MaintenanceModeEnabledException() : base(HttpStatusCode.ServiceUnavailable)
    {
    }
}
