/* 
 * Copyright (c) 2021-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

namespace Spark.Engine.Service.FhirServiceExtensions
{
    using Hl7.Fhir.Model;

    public interface IPatchService : IFhirServiceExtension
    {
        Resource Apply(Resource resource, Parameters patch);
    }
}