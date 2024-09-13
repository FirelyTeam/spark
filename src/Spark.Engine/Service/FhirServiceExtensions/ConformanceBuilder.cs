/* 
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Hl7.Fhir.Utility;

namespace Spark.Engine.Service.FhirServiceExtensions;

public static class CapabilityStatementBuilder
{
    public static CapabilityStatement GetSparkCapabilityStatement(string sparkVersion, ILocalhost localhost)
    {
        FHIRVersion vsn = FHIRVersion.N4_0_1;
        CapabilityStatement capabilityStatement = CreateServer("Spark", sparkVersion, "Incendi", fhirVersion: vsn);

        capabilityStatement.AddAllCoreResources(readhistory: true, updatecreate: true, versioning: CapabilityStatement.ResourceVersionPolicy.VersionedUpdate);
        capabilityStatement.AddAllSystemInteractions().AddAllInteractionsForAllResources().AddCoreSearchParamsAllResources();
        capabilityStatement.AddSummaryForAllResources();
        capabilityStatement.AddOperation("Fetch Patient Record", localhost.Absolute(new Uri("OperationDefinition/Patient-everything", UriKind.Relative)).ToString());
        capabilityStatement.AddOperation("Generate a Document", localhost.Absolute(new Uri("OperationDefinition/Composition-document", UriKind.Relative)).ToString());
        //capabilityStatement.AcceptUnknown = CapabilityStatement.UnknownContentCode.Both;
        capabilityStatement.Experimental = true;
        capabilityStatement.Kind = CapabilityStatementKind.Capability;
        capabilityStatement.Format = new string[] { "xml", "json" };
        capabilityStatement.Description = new Markdown("This FHIR SERVER is a reference Implementation server built in C# on HL7.Fhir.Core (nuget) by Firely, Incendi and others");

        return capabilityStatement;
    }

    public static CapabilityStatement CreateServer(string server, string serverVersion, string publisher, FHIRVersion fhirVersion)
    {
        CapabilityStatement capabilityStatement = new CapabilityStatement
        {
            Name = server,
            Publisher = publisher,
            Version = serverVersion,
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

    public static CapabilityStatement AddAllCoreResources(this CapabilityStatement capabilityStatement, Boolean readhistory, Boolean updatecreate, CapabilityStatement.ResourceVersionPolicy versioning)
    {
        foreach (var resource in ModelInfo.SupportedResources)
        {
            capabilityStatement.AddSingleResourceComponent(resource, readhistory, updatecreate, versioning);
        }
        return capabilityStatement;
    }

    public static CapabilityStatement AddMultipleResourceComponents(this CapabilityStatement capabilityStatement, List<string> resourcetypes, Boolean readhistory, Boolean updatecreate, CapabilityStatement.ResourceVersionPolicy versioning)
    {
        foreach (var type in resourcetypes)
        {
            AddSingleResourceComponent(capabilityStatement, type, readhistory, updatecreate, versioning);
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

    public static CapabilityStatement AddCoreSearchParamsAllResources(this CapabilityStatement capabilityStatement)
    {
        foreach (var r in capabilityStatement.Rest.FirstOrDefault().Resource.ToList())
        {
            capabilityStatement.Rest().Resource.Remove(r);
            capabilityStatement.Rest().Resource.Add(AddCoreSearchParamsResource(r));
        }
        return capabilityStatement;
    }


    public static CapabilityStatement.ResourceComponent AddCoreSearchParamsResource(CapabilityStatement.ResourceComponent resourcecomp)
    {
        var parameters = ModelInfo.SearchParameters.Where(sp => sp.Resource == resourcecomp.Type)
            .Select(sp => new CapabilityStatement.SearchParamComponent
            {
                Name = sp.Name,
                Type = sp.Type,
                Documentation = sp.Description
            });

        resourcecomp.SearchParam.AddRange(parameters);
        return resourcecomp;
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

    public static CapabilityStatement.ResourceComponent AddAllResourceInteractions(CapabilityStatement.ResourceComponent resourcecomp)
    {
        foreach (CapabilityStatement.TypeRestfulInteraction type in Enum.GetValues(typeof(CapabilityStatement.TypeRestfulInteraction)))
        {
            var interaction = AddSingleResourceInteraction(resourcecomp, type);
            resourcecomp.Interaction.Add(interaction);
        }
        return resourcecomp;
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

    public static void AddSystemInteraction(this CapabilityStatement capabilityStatement, CapabilityStatement.SystemRestfulInteraction code)
    {
        var interaction = new CapabilityStatement.SystemInteractionComponent
        {
            Code = code
        };

        capabilityStatement.Rest().Interaction.Add(interaction);
    }

    public static void AddOperation(this CapabilityStatement capabilityStatement, string name, string definition)
    {
        var operation = new CapabilityStatement.OperationComponent
        {
            Name = name,
            Definition = definition
        };

        capabilityStatement.Server().Operation.Add(operation);
    }
}