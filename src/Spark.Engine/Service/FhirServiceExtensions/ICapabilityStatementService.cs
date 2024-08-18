/* 
 * Copyright (c) 2016-2018, Firely (info@fire.ly) and contributors
 * Copyright (c) 2018-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface ICapabilityStatementService : IFhirServiceExtension
    {
        CapabilityStatement GetSparkCapabilityStatement(string sparkVersion);
    }
}