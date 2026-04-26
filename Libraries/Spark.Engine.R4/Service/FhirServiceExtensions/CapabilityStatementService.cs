/*
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using Hl7.Fhir.Model;
using Spark.Engine.Core;
using static Hl7.Fhir.Model.CapabilityStatement;
using CoreCapabilityStatementBuilder = Spark.Engine.Core.CapabilityStatementBuilder;

namespace Spark.Engine.Service.FhirServiceExtensions;

public class CapabilityStatementService : ICapabilityStatementService
{
    private readonly ILocalhost _localhost;
    private readonly IFhirModel _fhirModel;
    private readonly ServerVersion _serverVersion;
    private readonly FHIRVersion _fhirVersion;
    private CapabilityStatement _capabilityStatement;

    public CapabilityStatementService(ILocalhost localhost, IFhirModel fhirModel, ServerVersion serverVersion, FHIRVersion fhirVersion)
    {
        _localhost = localhost;
        _fhirModel = fhirModel;
        _serverVersion = serverVersion;
        _fhirVersion = fhirVersion;
    }

    public CapabilityStatement GetSparkCapabilityStatement()
    {
        return _capabilityStatement ??= BuildCapabilityStatement();
    }

    private CapabilityStatement BuildCapabilityStatement()
    {
        return new CoreCapabilityStatementBuilder()
            .WithName("Spark FHIR Server")
            .WithVersion(_serverVersion)
            .WithPublisher("Incendi")
            .WithDate(DateTimeOffset.UtcNow)
            .WithCopyright(
                "This server is Open Source Software, licensed under the terms of the [BSD-3-Clause License](https://raw.githubusercontent.com/FirelyTeam/spark/refs/heads/r4/master/LICENSE)")
            .WithExperimental(true)
            .WithKind(CapabilityStatementKind.Capability)
            .WithFhirVersion(_fhirVersion)
            .WithAcceptFormat(["xml", "json"])
            .WithRest(b =>
            {
                b.WithMode(RestfulCapabilityMode.Server);

                foreach (var resourceType in _fhirModel.SupportedResources)
                {
                    b.WithResource(r =>
                    {
                        r.WithType(resourceType)
                            .WithVersioning(ResourceVersionPolicy.VersionedUpdate)
                            .WithReadHistory(true)
                            .WithUpdateCreate(true);

                        foreach (TypeRestfulInteraction interaction in Enum.GetValues(typeof(TypeRestfulInteraction)))
                            r.WithInteraction(interaction);

                        foreach (var sp in _fhirModel.FindSearchParameters(resourceType))
                            r.WithSearchParam(sp.Name, sp.Type ?? SearchParamType.String,
                                documentation: sp.Description);

                        r.WithSearchParam("_summary", SearchParamType.String, documentation: "Summary for resource");

                        return r;
                    });
                }

                b.WithInteraction(SystemRestfulInteraction.Transaction)
                    .WithInteraction(SystemRestfulInteraction.Batch)
                    .WithInteraction(SystemRestfulInteraction.SearchSystem)
                    .WithInteraction(SystemRestfulInteraction.HistorySystem)
                    .WithOperation("Fetch Patient Record",
                        _localhost.Absolute(new Uri("OperationDefinition/Patient-everything", UriKind.Relative))
                            .ToString())
                    .WithOperation("Generate a Document",
                        _localhost.Absolute(new Uri("OperationDefinition/Composition-document", UriKind.Relative))
                            .ToString());

                return b;
            })
            .Build();
    }
}
