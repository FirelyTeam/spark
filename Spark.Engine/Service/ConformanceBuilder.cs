/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Hl7.Fhir.Serialization;
using System.Xml.Linq;
using Hl7.Fhir.Rest;

namespace Spark.Service
{
    public class ConformanceBuilder
    {
        private Conformance con;

        public ConformanceBuilder(String publisher, String fhirversion, Boolean acceptunknown, Conformance conformance = null)
        {
            con = (conformance != null) ? conformance : new Conformance();
            con.Publisher = publisher;
            con.FhirVersion = fhirversion;
            con.AcceptUnknown = acceptunknown;
            con.Date = Date.Today().Value;

            //AddRestComponent(true);
            //AddAllCoreResources(true, true, Conformance.ResourceVersionPolicy.VersionedUpdate);
            //AddAllSystemInteractions();
            //AddAllResourceInteractionsAllResources();
            //AddCoreSearchParamsAllResources();

            //return con;

        }

        public void AddRestComponent(Boolean isServer, String documentation = null, String mailbox = null)
        {
            var newrest = new Conformance.ConformanceRestComponent();
            newrest.Mode = (isServer) ? Conformance.RestfulConformanceMode.Server : Conformance.RestfulConformanceMode.Client;

            if (documentation != null)
            {
                newrest.Documentation = documentation;
            }

            if (mailbox != null)
            {
                var listmailbox = (List<String>)newrest.DocumentMailbox;
                listmailbox.Add(mailbox);
                newrest.DocumentMailbox = listmailbox;
            }
            con.Rest.Add(newrest);
        }

        public void AddAllCoreResources(Boolean readhistory, Boolean updatecreate, Conformance.ResourceVersionPolicy versioning)
        {
            foreach (var resource in ModelInfo.SupportedResources)
            {
                AddSingleResourceComponent(resource, readhistory, updatecreate, versioning);
            }
        }

        public void AddMultipleResourceComponents(List<String> resourcetypes, Boolean readhistory, Boolean updatecreate, Conformance.ResourceVersionPolicy versioning)
        {
            foreach (var type in resourcetypes)
            {
                AddSingleResourceComponent(type, readhistory, updatecreate, versioning);
            }
        }

        public void AddSingleResourceComponent(String resourcetype, Boolean readhistory, Boolean updatecreate, Conformance.ResourceVersionPolicy versioning, ResourceReference profile = null)
        {
            var resourcecomponent = new Conformance.ConformanceRestResourceComponent();
            resourcecomponent.Type = resourcetype;
            resourcecomponent.Profile = profile;
            resourcecomponent.ReadHistory = readhistory;
            resourcecomponent.UpdateCreate = updatecreate;
            resourcecomponent.Versioning = versioning;
            con.Rest.FirstOrDefault().Resource.Add(resourcecomponent);
        }

        public void AddCoreSearchParamsAllResources()
        {
            foreach (var r in con.Rest.FirstOrDefault().Resource.ToList())
            {
                con.Rest.FirstOrDefault().Resource.Remove(r);
                con.Rest.FirstOrDefault().Resource.Add(AddCoreSearchParamsResource(r));
            }
        }

        public Conformance.ConformanceRestResourceComponent AddCoreSearchParamsResource(Conformance.ConformanceRestResourceComponent resourcecomp)
        {
            var parameters = ModelInfo.SearchParameters.Where(sp => sp.Resource == resourcecomp.Type)
                            .Select(sp => new Conformance.ConformanceRestResourceSearchParamComponent
                            {
                                Name = sp.Name,
                                Type = sp.Type,
                                Documentation = sp.Description,
                            });

            resourcecomp.SearchParam.AddRange(parameters);
            return resourcecomp;
        }

        public void AddAllResourceInteractionsAllResources()
        {
            foreach (var r in con.Rest.FirstOrDefault().Resource.ToList())
            {
                con.Rest.FirstOrDefault().Resource.Remove(r);
                con.Rest.FirstOrDefault().Resource.Add(AddAllResourceInteractions(r));
            }
        }

        public Conformance.ConformanceRestResourceComponent AddAllResourceInteractions(Conformance.ConformanceRestResourceComponent resourcecomp)
        {
            foreach (Conformance.TypeRestfulInteraction type in Enum.GetValues(typeof(Conformance.TypeRestfulInteraction)))
            {
                var interaction = AddSingleResourceInteraction(resourcecomp, type);
                resourcecomp.Interaction.Add(interaction);
            }
            return resourcecomp;
        }

        public Conformance.ResourceInteractionComponent AddSingleResourceInteraction(Conformance.ConformanceRestResourceComponent resourcecomp, Conformance.TypeRestfulInteraction type)
        {
            var interaction = new Conformance.ResourceInteractionComponent();
            interaction.Code = type;
            return interaction;

        }


        public void AddAllSystemInteractions()
        {
            foreach (Conformance.SystemRestfulInteraction code in Enum.GetValues(typeof(Conformance.SystemRestfulInteraction)))
            {
                AddSystemInteraction(code);
            }
        }

        public void AddSystemInteraction(Conformance.SystemRestfulInteraction code)
        {
            var interaction = new Conformance.SystemInteractionComponent();

            interaction.Code = code;

            con.Rest.FirstOrDefault().Interaction.Add(interaction);
        }

        public void AddOperation(String name, ResourceReference definition)
        {
            var operation = new Conformance.ConformanceRestOperationComponent();

            operation.Name = name;
            operation.Definition = definition;

            con.Rest.FirstOrDefault().Operation.Add(operation);
        }

        public String ConformanceToXML()
        {
            return FhirSerializer.SerializeResourceToXml(con);
        }

        public Conformance GenerateConformance()
        {
            return con;
        }
    }

}
        //public const string CONFORMANCE_ID = "self";
        //public static readonly string CONFORMANCE_COLLECTION_NAME = typeof(Conformance).GetCollectionName();
    
        //public static Conformance CreateTemp()
        //{
        //    return new Conformance();

        //}

        //public static Conformance Build()
        //{
        //    //var conformance = new Conformance();

            // DSTU2: Conformance

            //Stream s = typeof(ConformanceBuilder).Assembly.GetManifestResourceStream("Spark.Engine.Service.Conformance.xml");
            //StreamReader sr = new StreamReader(s);
            //string conformanceXml = sr.ReadToEnd();
            
            //var conformance = (Conformance)FhirParser.ParseResourceFromXml(conformanceXml);

            //conformance.Software = new Conformance.ConformanceSoftwareComponent();
            //conformance.Software.Version = ReadVersionFromAssembly();
            //conformance.Software.Name = ReadProductNameFromAssembly();
            //conformance.FhirVersion = ModelInfo.Version;
            //conformance.Date = Date.Today().Value;
            //conformance.Meta = new Resource.ResourceMetaComponent();
            //conformance.Meta.VersionId = "0";

            //Conformance.ConformanceRestComponent serverComponent = conformance.Rest[0];

            // Replace anything that was there before...
            //serverComponent.Resource = new List<Conformance.ConformanceRestResourceComponent>();

            // todo: An alternative is needed for the missing operation types below:
                
            /*var allOperations = new List<Conformance.ConformanceRestResourceOperationComponent>()
            {   new Conformance.ConformanceRestResourceOperationComponent { Code = Conformance.RestfulOperationType.Create },
                new Conformance.ConformanceRestResourceOperationComponent { Code = Conformance.RestfulOperationType.Delete },
                new Conformance.ConformanceRestResourceOperationComponent { Code = Conformance.RestfulOperationType.HistoryInstance },

                new Conformance.ConformanceRestResourceOperationComponent { Code = Conformance.RestfulOperationType.HistoryType },
                new Conformance.ConformanceRestResourceOperationComponent { Code = Conformance.RestfulOperationType.Read },
                
                new Conformance.ConformanceRestResourceOperationComponent { Code = Conformance.RestfulOperationType.SearchType },
                
                
                new Conformance.ConformanceRestResourceOperationComponent { Code = Conformance.RestfulOperationType.Update },
                new Conformance.ConformanceRestResourceOperationComponent { Code = Conformance.RestfulOperationType.Validate },            
                new Conformance.ConformanceRestResourceOperationComponent { Code = Conformance.RestfulOperationType.Vread },
            };

            foreach (var resourceName in ModelInfo.SupportedResources)
            {
                var supportedResource = new Conformance.ConformanceRestResourceComponent();
                supportedResource.Type = resourceName;
                supportedResource.ReadHistory = true;
                supportedResource.Operation = allOperations;

                // Add supported _includes for this resource
                supportedResource.SearchInclude = ModelInfo.SearchParameters
                    .Where(sp => sp.Resource == resourceName)
                    .Where(sp => sp.Type == Conformance.SearchParamType.Reference)
                    .Select(sp => sp.Name);

                supportedResource.SearchParam = new List<Conformance.ConformanceRestResourceSearchParamComponent>();

                // todo: search params. error: "The name "Search" does not exist in the current context
                var parameters = ModelInfo.SearchParameters.Where(sp => sp.Resource == resourceName)
                        .Select(sp => new Conformance.ConformanceRestResourceSearchParamComponent
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

    //        conformance.Text = new Narrative();
    //        conformance.Text.Div = summary.ToString();
    //        return conformance;
    //    }

    //    public static string ReadVersionFromAssembly()
    //    {
    //        var attribute = (System.Reflection.AssemblyFileVersionAttribute)typeof(ConformanceBuilder).Assembly
    //            .GetCustomAttributes(typeof(System.Reflection.AssemblyFileVersionAttribute), true)
    //            .Single();
    //        return attribute.Version;
    //    }

    //    public static string ReadProductNameFromAssembly()
    //    {
    //        var attribute = (System.Reflection.AssemblyProductAttribute)typeof(ConformanceBuilder).Assembly
    //            .GetCustomAttributes(typeof(System.Reflection.AssemblyProductAttribute), true)
    //            .Single();
    //        return attribute.Product;
    //    }
    //}
 
//}