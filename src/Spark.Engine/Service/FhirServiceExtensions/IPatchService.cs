/* 
 * Copyright (c) 2021-2024, Incendi (info@incendi.no)
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

namespace Spark.Engine.Service.FhirServiceExtensions
{
    using Hl7.Fhir.Model;

    public interface IPatchService : IFhirServiceExtension
    {
        Resource Apply(Resource resource, Parameters patch);
    }
}