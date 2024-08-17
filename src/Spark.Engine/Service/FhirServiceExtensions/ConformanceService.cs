/* 
 * Copyright (c) 2016-2018, Furore (info@furore.com) and contributors
 * Copyright (c) 2018-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public class CapabilityStatementService : ICapabilityStatementService
    {
        private readonly ILocalhost _localhost;

        public CapabilityStatementService(ILocalhost localhost)
        {
            _localhost = localhost;
        }

        public CapabilityStatement GetSparkCapabilityStatement(string sparkVersion)
        {
           return CapabilityStatementBuilder.GetSparkCapabilityStatement(sparkVersion, _localhost);
        }
    }
}