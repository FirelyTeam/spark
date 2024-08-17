/* 
 * Copyright (c) 2020-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Spark.Engine.Core;
using System.Net;

namespace Spark.Engine.Maintenance
{
    internal class MaintenanceModeEnabledException : SparkException
    {
        public MaintenanceModeEnabledException() : base(HttpStatusCode.ServiceUnavailable)
        {
        }
    }
}
