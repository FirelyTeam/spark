/* 
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions;

public class CapabilityStatementService : ICapabilityStatementService
{
    private readonly ILocalhost _localhost;
    private readonly IFhirModel _fhirModel;

    public CapabilityStatementService(ILocalhost localhost, IFhirModel fhirModel)
    {
        _localhost = localhost;
        _fhirModel = fhirModel;
    }

    public CapabilityStatement GetSparkCapabilityStatement(string sparkVersion)
    {
        return CapabilityStatementBuilder.GetCapabilityStatement(sparkVersion, _localhost, _fhirModel);
    }
}
