/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Spark.Engine.Core;
using Hl7.Fhir.Utility;

namespace Spark.Engine.Service.FhirServiceExtensions
{

    public static class CapabilityStatementBuilder
    {
        public static CapabilityStatement GetSparkCapabilityStatement(string sparkVersion, ILocalhost localhost)
        {
            FHIRVersion vsn = FHIRVersion.N4_0_0;
            CapabilityStatement capabilityStatement = CreateServer("Spark", sparkVersion, "Kufu", fhirVersion: vsn);

            capabilityStatement.AddAllCoreResources(readhistory: true, updatecreate: true, versioning: CapabilityStatement.ResourceVersionPolicy.VersionedUpdate);
            capabilityStatement.AddAllSystemInteractions().AddAllInteractionsForAllResources().AddCoreSearchParamsAllResources();
            capabilityStatement.AddSummaryForAllResources();
            capabilityStatement.AddOperation("Fetch Patient Record", localhost.Absolute(new Uri("OperationDefinition/Patient-everything", UriKind.Relative)).ToString());
            capabilityStatement.AddOperation("Generate a Document", localhost.Absolute(new Uri("OperationDefinition/Composition-document", UriKind.Relative)).ToString());
            //capabilityStatement.AcceptUnknown = CapabilityStatement.UnknownContentCode.Both;
            capabilityStatement.Experimental = true;
            capabilityStatement.Kind = CapabilityStatementKind.Capability;
            capabilityStatement.Format = new string[] { "xml", "json" };
            capabilityStatement.Description = new Markdown("This FHIR SERVER is a reference Implementation server built in C# on HL7.Fhir.Core (nuget) by Furore and others");

            return capabilityStatement;
        }

        public static CapabilityStatement CreateServer(String server, String serverVersion, String publisher, FHIRVersion fhirVersion)
        {
            CapabilityStatement capabilityStatement = new CapabilityStatement();
            capabilityStatement.Name = server;
            capabilityStatement.Publisher = publisher;
            capabilityStatement.Version = serverVersion;
            capabilityStatement.FhirVersion = fhirVersion;
            //capabilityStatement.AcceptUnknown = CapabilityStatement.UnknownContentCode.No;
            capabilityStatement.Date = Date.Today().Value;
            capabilityStatement.AddServer();
            return capabilityStatement;
            //AddRestComponent(true);
            //AddAllCoreResources(true, true, CapabilityStatement.ResourceVersionPolicy.VersionedUpdate);
            //AddAllSystemInteractions();
            //AddAllResourceInteractionsAllResources();
            //AddCoreSearchParamsAllResources();

            //return con;
        }

        public static CapabilityStatement.RestComponent AddRestComponent(this CapabilityStatement capabilityStatement, Boolean isServer, Markdown documentation = null)
        {
            var server = new CapabilityStatement.RestComponent();
            server.Mode = (isServer) ? CapabilityStatement.RestfulCapabilityMode.Server : CapabilityStatement.RestfulCapabilityMode.Client;

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
            return (server == null) ? capabilityStatement.AddRestComponent(isServer: true) : server;
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

        public static CapabilityStatement AddMultipleResourceComponents(this CapabilityStatement capabilityStatement, List<String> resourcetypes, Boolean readhistory, Boolean updatecreate, CapabilityStatement.ResourceVersionPolicy versioning)
        {
            foreach (var type in resourcetypes)
            {
                AddSingleResourceComponent(capabilityStatement, type, readhistory, updatecreate, versioning);
            }
            return capabilityStatement;
        }

        public static CapabilityStatement AddSingleResourceComponent(this CapabilityStatement capabilityStatement, String resourcetype, Boolean readhistory, Boolean updatecreate, CapabilityStatement.ResourceVersionPolicy versioning, Canonical profile = null)
        {
            var resource = new CapabilityStatement.ResourceComponent();

            resource.Type = Hacky.GetResourceTypeForResourceName(resourcetype);
            resource.Profile = profile;
            resource.ReadHistory = readhistory;
            resource.UpdateCreate = updatecreate;
            resource.Versioning = versioning;
            capabilityStatement.Server().Resource.Add(resource);
            return capabilityStatement;
        }

        public static CapabilityStatement AddSummaryForAllResources(this CapabilityStatement capabilityStatement)
        {
            foreach (var resource in capabilityStatement.Rest.FirstOrDefault().Resource.ToList())
            {
                var p = new CapabilityStatement.SearchParamComponent();
                p.Name = "_summary";
                p.Type = SearchParamType.String;
                p.Documentation = new Markdown("Summary for resource");
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
            var parameters = ModelInfo.SearchParameters.Where(sp => sp.Resource == resourcecomp.Type.GetLiteral())
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
            var interaction = new CapabilityStatement.ResourceInteractionComponent();
            interaction.Code = type;
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
            var interaction = new CapabilityStatement.SystemInteractionComponent();

            interaction.Code = code;

            capabilityStatement.Rest().Interaction.Add(interaction);
        }

        public static void AddOperation(this CapabilityStatement capabilityStatement, String name, string definition)
        {
            var operation = new CapabilityStatement.OperationComponent();

            operation.Name = name;
            operation.Definition = definition;

            capabilityStatement.Server().Operation.Add(operation);
        }
    }
}

// TODO: Code review CapabilityStatement replacement
//public const string CONFORMANCE_ID = "self";
//public static readonly string CONFORMANCE_COLLECTION_NAME = typeof(CapabilityStatement).GetCollectionName();

//public static CapabilityStatement CreateTemp()
//{
//    return new CapabilityStatement();

//}

//public static CapabilityStatement Build()
//{
//    //var capabilityStatement = new CapabilityStatement();

//Stream s = typeof(CapabilityStatementBuilder).Assembly.GetManifestResourceStream("Spark.Engine.Service.CapabilityStatement.xml");
//StreamReader sr = new StreamReader(s);
//string capabilityStatementXml = sr.ReadToEnd();

//var capabilityStatement = (CapabilityStatement)FhirParser.ParseResourceFromXml(capabilityStatementXml);

//capabilityStatement.Software = new CapabilityStatement.CapabilityStatementSoftwareComponent();
//capabilityStatement.Software.Version = ReadVersionFromAssembly();
//capabilityStatement.Software.Name = ReadProductNameFromAssembly();
//capabilityStatement.FhirVersion = ModelInfo.Version;
//capabilityStatement.Date = Date.Today().Value;
//capabilityStatement.Meta = new Resource.ResourceMetaComponent();
//capabilityStatement.Meta.VersionId = "0";

//CapabilityStatement.CapabilityStatementRestComponent serverComponent = capabilityStatement.Rest[0];

// Replace anything that was there before...
//serverComponent.Resource = new List<CapabilityStatement.CapabilityStatementRestResourceComponent>();

/*var allOperations = new List<CapabilityStatement.CapabilityStatementRestResourceOperationComponent>()
{   new CapabilityStatement.CapabilityStatementRestResourceOperationComponent { Code = CapabilityStatement.RestfulOperationType.Create },
    new CapabilityStatement.CapabilityStatementRestResourceOperationComponent { Code = CapabilityStatement.RestfulOperationType.Delete },
    new CapabilityStatement.CapabilityStatementRestResourceOperationComponent { Code = CapabilityStatement.RestfulOperationType.HistoryInstance },

    new CapabilityStatement.CapabilityStatementRestResourceOperationComponent { Code = CapabilityStatement.RestfulOperationType.HistoryType },
    new CapabilityStatement.CapabilityStatementRestResourceOperationComponent { Code = CapabilityStatement.RestfulOperationType.Read },

    new CapabilityStatement.CapabilityStatementRestResourceOperationComponent { Code = CapabilityStatement.RestfulOperationType.SearchType },


    new CapabilityStatement.CapabilityStatementRestResourceOperationComponent { Code = CapabilityStatement.RestfulOperationType.Update },
    new CapabilityStatement.CapabilityStatementRestResourceOperationComponent { Code = CapabilityStatement.RestfulOperationType.Validate },            
    new CapabilityStatement.CapabilityStatementRestResourceOperationComponent { Code = CapabilityStatement.RestfulOperationType.Vread },
};

foreach (var resourceName in ModelInfo.SupportedResources)
{
    var supportedResource = new CapabilityStatement.CapabilityStatementRestResourceComponent();
    supportedResource.Type = resourceName;
    supportedResource.ReadHistory = true;
    supportedResource.Operation = allOperations;

    // Add supported _includes for this resource
    supportedResource.SearchInclude = ModelInfo.SearchParameters
        .Where(sp => sp.Resource == resourceName)
        .Where(sp => sp.Type == CapabilityStatement.SearchParamType.Reference)
        .Select(sp => sp.Name);

    supportedResource.SearchParam = new List<CapabilityStatement.CapabilityStatementRestResourceSearchParamComponent>();


    var parameters = ModelInfo.SearchParameters.Where(sp => sp.Resource == resourceName)
            .Select(sp => new CapabilityStatement.CapabilityStatementRestResourceSearchParamComponent
                {
                    Name = sp.Name,
                    Type = sp.Type,
                    Documentation = sp.Description,
                });

    supportedResource.SearchParam.AddRange(parameters);

    serverComponent.Resource.Add(supportedResource);
}
*/
// This constant has become internal. Please undo. We need it. 

// Update: new location: XHtml.XHTMLNS / XHtml
//        // XNamespace ns = Hl7.Fhir.Support.Util.XHTMLNS;
//        XNamespace ns = "http://www.w3.org/1999/xhtml";

//        var summary = new XElement(ns + "div");

//        foreach (string resourceName in ModelInfo.SupportedResources)
//        {
//            summary.Add(new XElement(ns + "p",
//                String.Format("The server supports all operations on the {0} resource, including history",
//                    resourceName)));
//        }

//        capabilityStatement.Text = new Narrative();
//        capabilityStatement.Text.Div = summary.ToString();
//        return capabilityStatement;
//    }

//    public static string ReadVersionFromAssembly()
//    {
//        var attribute = (System.Reflection.AssemblyFileVersionAttribute)typeof(CapabilityStatementBuilder).Assembly
//            .GetCustomAttributes(typeof(System.Reflection.AssemblyFileVersionAttribute), true)
//            .Single();
//        return attribute.Version;
//    }

//    public static string ReadProductNameFromAssembly()
//    {
//        var attribute = (System.Reflection.AssemblyProductAttribute)typeof(CapabilityStatementBuilder).Assembly
//            .GetCustomAttributes(typeof(System.Reflection.AssemblyProductAttribute), true)
//            .Single();
//        return attribute.Product;
//    }
//}

//}