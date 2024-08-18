/* 
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2024, Incendi <info@incendi.no>
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