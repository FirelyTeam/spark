/* 
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions;

public static class CapabilityStatementBuilder
{
    public static CapabilityStatement GetCapabilityStatement(string serverVersion, ILocalhost localhost, IFhirModel fhirModel)
    {
        const FHIRVersion fhirVersion = FHIRVersion.N4_0_1;
        var capabilityStatement = CreateServer("Spark", serverVersion, "Incendi", fhirVersion: fhirVersion)
            .AddMultipleResourceComponents(
                resourceTypes: fhirModel.SupportedResources.ToList(),
                readHistory: true,
                updateCreate: true,
                versioning: CapabilityStatement.ResourceVersionPolicy.VersionedUpdate
            )
            .AddAllSystemInteractions()
            .AddAllInteractionsForAllResources()
            .AddSearchParametersForAllResources()
            .AddSummaryForAllResources()
            .AddOperation(
                name: "Fetch Patient Record",
                definition: localhost.Absolute(new Uri("OperationDefinition/Patient-everything", UriKind.Relative)).ToString()
            )
            .AddOperation(
                name: "Generate a Document",
                definition: localhost.Absolute(new Uri("OperationDefinition/Composition-document", UriKind.Relative)).ToString()
            );

        capabilityStatement.Experimental = true;
        capabilityStatement.Kind = CapabilityStatementKind.Capability;
        capabilityStatement.Format = ["xml", "json"];
        capabilityStatement.Description = new Markdown("This FHIR SERVER is a reference Implementation server built in C# on HL7.Fhir.Core (nuget) by Firely, Incendi and others");

        return capabilityStatement;
    }

    public static CapabilityStatement CreateServer(string name, string version, string publisher, FHIRVersion fhirVersion)
    {
        CapabilityStatement capabilityStatement = new CapabilityStatement
        {
            Name = name,
            Publisher = publisher,
            Version = version,
            FhirVersion = fhirVersion,
            Date = Date.Today().Value
        };
        capabilityStatement.AddServer();
        return capabilityStatement;
    }

    public static CapabilityStatement.RestComponent AddRestComponent(this CapabilityStatement capabilityStatement, bool isServer, Markdown documentation = null)
    {
        var server = new CapabilityStatement.RestComponent
        {
            Mode = (isServer) ? CapabilityStatement.RestfulCapabilityMode.Server : CapabilityStatement.RestfulCapabilityMode.Client
        };

        if (documentation != null)
        {
            server.Documentation = documentation;
        }
        capabilityStatement.Rest.Add(server);
        return server;
    }

    public static CapabilityStatement AddServer(this CapabilityStatement capabilityStatement)
    {
        capabilityStatement.AddRestComponent(isServer: true);
        return capabilityStatement;
    }

    public static CapabilityStatement.RestComponent Server(this CapabilityStatement capabilityStatement)
    {
        var server = capabilityStatement.Rest.FirstOrDefault(r => r.Mode == CapabilityStatement.RestfulCapabilityMode.Server);
        return server ?? capabilityStatement.AddRestComponent(isServer: true);
    }

    public static CapabilityStatement.RestComponent Rest(this CapabilityStatement capabilityStatement)
    {
        return capabilityStatement.Rest.FirstOrDefault();
    }

    public static CapabilityStatement AddMultipleResourceComponents(this CapabilityStatement capabilityStatement, List<string> resourceTypes, bool readHistory, bool updateCreate, CapabilityStatement.ResourceVersionPolicy versioning)
    {
        foreach (var type in resourceTypes)
        {
            capabilityStatement.AddSingleResourceComponent(type, readHistory, updateCreate, versioning);
        }
        return capabilityStatement;
    }

    public static CapabilityStatement AddSingleResourceComponent(this CapabilityStatement capabilityStatement, string resourcetype, bool readhistory, bool updatecreate, CapabilityStatement.ResourceVersionPolicy versioning, Canonical profile = null)
    {
        var resource = new CapabilityStatement.ResourceComponent
        {
            Type = resourcetype,
            Profile = profile,
            ReadHistory = readhistory,
            UpdateCreate = updatecreate,
            Versioning = versioning
        };
        capabilityStatement.Server().Resource.Add(resource);
        return capabilityStatement;
    }

    public static CapabilityStatement AddSummaryForAllResources(this CapabilityStatement capabilityStatement)
    {
        foreach (var resource in capabilityStatement.Rest.FirstOrDefault().Resource.ToList())
        {
            var p = new CapabilityStatement.SearchParamComponent
            {
                Name = "_summary",
                Type = SearchParamType.String,
                Documentation = new Markdown("Summary for resource")
            };
            resource.SearchParam.Add(p);
        }
        return capabilityStatement;
    }

    public static CapabilityStatement AddSearchParametersForAllResources(this CapabilityStatement capabilityStatement)
    {
        foreach (var resourceComponent in capabilityStatement.Rest.FirstOrDefault().Resource.ToList())
        {
            capabilityStatement.Rest().Resource.Remove(resourceComponent);
            capabilityStatement.Rest().Resource.Add(AddSearchParametersForResource(resourceComponent));
        }
        return capabilityStatement;
    }

    public static CapabilityStatement.ResourceComponent AddSearchParametersForResource(CapabilityStatement.ResourceComponent resourceComponent)
    {
        var parameters = ModelInfo.SearchParameters.Where(sp => sp.Resource == resourceComponent.Type)
            .Select(sp => new CapabilityStatement.SearchParamComponent
            {
                Name = sp.Name,
                Type = sp.Type,
                Documentation = sp.Description
            });

        resourceComponent.SearchParam.AddRange(parameters);
        return resourceComponent;
    }

    public static CapabilityStatement AddAllInteractionsForAllResources(this CapabilityStatement capabilityStatement)
    {
        foreach (var r in capabilityStatement.Rest.FirstOrDefault().Resource.ToList())
        {
            capabilityStatement.Rest().Resource.Remove(r);
            capabilityStatement.Rest().Resource.Add(AddAllResourceInteractions(r));
        }
        return capabilityStatement;
    }

    public static CapabilityStatement.ResourceComponent AddAllResourceInteractions(CapabilityStatement.ResourceComponent resourceComponent)
    {
        foreach (CapabilityStatement.TypeRestfulInteraction type in Enum.GetValues(typeof(CapabilityStatement.TypeRestfulInteraction)))
        {
            var interaction = AddSingleResourceInteraction(resourceComponent, type);
            resourceComponent.Interaction.Add(interaction);
        }
        return resourceComponent;
    }

    public static CapabilityStatement.ResourceInteractionComponent AddSingleResourceInteraction(CapabilityStatement.ResourceComponent resourcecomp, CapabilityStatement.TypeRestfulInteraction type)
    {
        var interaction = new CapabilityStatement.ResourceInteractionComponent
        {
            Code = type
        };
        return interaction;

    }

    public static CapabilityStatement AddAllSystemInteractions(this CapabilityStatement capabilityStatement)
    {
        foreach (CapabilityStatement.SystemRestfulInteraction code in Enum.GetValues(typeof(CapabilityStatement.SystemRestfulInteraction)))
        {
            capabilityStatement.AddSystemInteraction(code);
        }
        return capabilityStatement;
    }

    public static CapabilityStatement AddSystemInteraction(this CapabilityStatement capabilityStatement, CapabilityStatement.SystemRestfulInteraction code)
    {
        var interaction = new CapabilityStatement.SystemInteractionComponent
        {
            Code = code
        };

        capabilityStatement.Rest().Interaction.Add(interaction);

        return capabilityStatement;
    }

    public static CapabilityStatement AddOperation(this CapabilityStatement capabilityStatement, string name, string definition)
    {
        var operation = new CapabilityStatement.OperationComponent
        {
            Name = name,
            Definition = definition
        };

        capabilityStatement.Server().Operation.Add(operation);

        return capabilityStatement;
    }
}
