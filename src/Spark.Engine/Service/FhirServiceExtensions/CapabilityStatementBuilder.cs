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
using Hl7.Fhir.Introspection;
using Hl7.Fhir.Utility;

namespace Spark.Engine.Service.FhirServiceExtensions
{

	public static class CapabilityStatementBuilder
	{
		public static CapabilityStatement GetSparkCapabilityStatement( string sparkVersion, ILocalhost localhost )
		{
			string vsn = Hl7.Fhir.Model.ModelInfo.Version;
			CapabilityStatement conformance = CreateServer( "Spark", sparkVersion, "Furore", fhirVersion: vsn );

			conformance.AddAllCoreResources( readhistory: true, updatecreate: true, versioning: CapabilityStatement.ResourceVersionPolicy.VersionedUpdate );
			conformance.AddAllSystemInteractions().AddAllInteractionsForAllResources().AddCoreSearchParamsAllResources();
			conformance.AddSummaryForAllResources();
			conformance.AddOperation( "Fetch Patient Record", new ResourceReference() { Url = localhost.Absolute( new Uri( "OperationDefinition/Patient-everything", UriKind.Relative ) ) } );
			conformance.AddOperation( "Generate a Document", new ResourceReference() { Url = localhost.Absolute( new Uri( "OperationDefinition/Composition-document", UriKind.Relative ) ) } );

			conformance.AcceptUnknown = CapabilityStatement.UnknownContentCode.Both;
			conformance.Experimental = true;
			conformance.Format = new string[] { "xml", "json" };
			conformance.Description = new Markdown( "This FHIR SERVER is a reference Implementation server built in C# on HL7.Fhir.Core (nuget) by Furore and others" );

			return conformance;
		}

		public static CapabilityStatement CreateServer( String server, String serverVersion, String publisher, String fhirVersion )
		{
			CapabilityStatement conformance = new CapabilityStatement();
			conformance.Name = server;
			conformance.Publisher = publisher;
			conformance.Version = serverVersion;
			conformance.FhirVersion = fhirVersion;
			conformance.AcceptUnknown = CapabilityStatement.UnknownContentCode.No;
			conformance.Date = Date.Today().Value;
			conformance.AddServer();
			return conformance;
			//AddRestComponent(true);
			//AddAllCoreResources(true, true, CapabilityStatement.ResourceVersionPolicy.VersionedUpdate);
			//AddAllSystemInteractions();
			//AddAllResourceInteractionsAllResources();
			//AddCoreSearchParamsAllResources();

			//return con;
		}

		public static CapabilityStatement.RestComponent AddRestComponent( this CapabilityStatement conformance, Boolean isServer, String documentation = null )
		{
			var server = new CapabilityStatement.RestComponent();
			server.Mode = (isServer) ? CapabilityStatement.RestfulCapabilityMode.Server : CapabilityStatement.RestfulCapabilityMode.Client;

			if( documentation != null )
			{
				server.Documentation = documentation;
			}
			conformance.Rest.Add( server );
			return server;
		}

		public static CapabilityStatement AddServer( this CapabilityStatement conformance )
		{
			conformance.AddRestComponent( isServer: true );
			return conformance;
		}

		public static CapabilityStatement.RestComponent Server( this CapabilityStatement conformance )
		{
			var server = conformance.Rest.FirstOrDefault( r => r.Mode == CapabilityStatement.RestfulCapabilityMode.Server );
			return (server == null) ? conformance.AddRestComponent( isServer: true ) : server;
		}

		public static CapabilityStatement.RestComponent Rest( this CapabilityStatement conformance )
		{
			return conformance.Rest.FirstOrDefault();
		}

		public static CapabilityStatement AddAllCoreResources( this CapabilityStatement conformance, Boolean readhistory, Boolean updatecreate, CapabilityStatement.ResourceVersionPolicy versioning )
		{
			foreach( var resource in ModelInfo.SupportedResources )
			{
				conformance.AddSingleResourceComponent( resource, readhistory, updatecreate, versioning );
			}
			return conformance;
		}

		public static CapabilityStatement AddMultipleResourceComponents( this CapabilityStatement conformance, List<String> resourcetypes, Boolean readhistory, Boolean updatecreate, CapabilityStatement.ResourceVersionPolicy versioning )
		{
			foreach( var type in resourcetypes )
			{
				AddSingleResourceComponent( conformance, type, readhistory, updatecreate, versioning );
			}
			return conformance;
		}

		public static CapabilityStatement AddSingleResourceComponent( this CapabilityStatement conformance, String resourcetype, Boolean readhistory, Boolean updatecreate, CapabilityStatement.ResourceVersionPolicy versioning, ResourceReference profile = null )
		{
			var resource = new CapabilityStatement.ResourceComponent();

			resource.Type = Hacky.GetResourceTypeForResourceName( resourcetype );
			resource.Profile = profile;
			resource.ReadHistory = readhistory;
			resource.UpdateCreate = updatecreate;
			resource.Versioning = versioning;
			conformance.Server().Resource.Add( resource );
			return conformance;
		}

		public static CapabilityStatement AddSummaryForAllResources( this CapabilityStatement conformance )
		{
			foreach( var resource in conformance.Rest.FirstOrDefault().Resource.ToList() )
			{
				var p = new CapabilityStatement.SearchParamComponent();
				p.Name = "_summary";
				p.Type = SearchParamType.String;
				p.Documentation = "Summary for resource";
				resource.SearchParam.Add( p );
			}
			return conformance;
		}

		public static CapabilityStatement AddCoreSearchParamsAllResources( this CapabilityStatement conformance )
		{
			foreach( var r in conformance.Rest.FirstOrDefault().Resource.ToList() )
			{
				conformance.Rest().Resource.Remove( r );
				conformance.Rest().Resource.Add( AddCoreSearchParamsResource( r ) );
			}
			return conformance;
		}


		public static CapabilityStatement.ResourceComponent AddCoreSearchParamsResource( CapabilityStatement.ResourceComponent resourcecomp )
		{
			var parameters = ModelInfo.SearchParameters.Where( sp => sp.Resource == resourcecomp.Type?.GetLiteral() )
							.Select( sp => new CapabilityStatement.SearchParamComponent
							{
								Name = sp.Name,
								Type = sp.Type,
								Documentation = sp.Description,

							} );

			resourcecomp.SearchParam.AddRange( parameters );
			return resourcecomp;
		}

		public static CapabilityStatement AddAllInteractionsForAllResources( this CapabilityStatement conformance )
		{
			foreach( var r in conformance.Rest.FirstOrDefault().Resource.ToList() )
			{
				conformance.Rest().Resource.Remove( r );
				conformance.Rest().Resource.Add( AddAllResourceInteractions( r ) );
			}
			return conformance;
		}

		public static CapabilityStatement.ResourceComponent AddAllResourceInteractions( CapabilityStatement.ResourceComponent resourcecomp )
		{
			foreach( CapabilityStatement.TypeRestfulInteraction type in Enum.GetValues( typeof( CapabilityStatement.TypeRestfulInteraction ) ) )
			{
				var interaction = AddSingleResourceInteraction( resourcecomp, type );
				resourcecomp.Interaction.Add( interaction );
			}
			return resourcecomp;
		}

		public static CapabilityStatement.ResourceInteractionComponent AddSingleResourceInteraction( CapabilityStatement.ResourceComponent resourcecomp, CapabilityStatement.TypeRestfulInteraction type )
		{
			var interaction = new CapabilityStatement.ResourceInteractionComponent();
			interaction.Code = type;
			return interaction;

		}

		public static CapabilityStatement AddAllSystemInteractions( this CapabilityStatement conformance )
		{
			foreach( CapabilityStatement.SystemRestfulInteraction code in Enum.GetValues( typeof( CapabilityStatement.SystemRestfulInteraction ) ) )
			{
				conformance.AddSystemInteraction( code );
			}
			return conformance;
		}

		public static void AddSystemInteraction( this CapabilityStatement conformance, CapabilityStatement.SystemRestfulInteraction code )
		{
			var interaction = new CapabilityStatement.SystemInteractionComponent();

			interaction.Code = code;

			conformance.Rest().Interaction.Add( interaction );
		}

		public static void AddOperation( this CapabilityStatement conformance, String name, ResourceReference definition )
		{
			var operation = new CapabilityStatement.OperationComponent();

			operation.Name = name;
			operation.Definition = definition;

			conformance.Server().Operation.Add( operation );
		}

		public static String CapabilityStatementToXML( this CapabilityStatement conformance )
		{
			return FhirSerializer.SerializeResourceToXml( conformance );
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
        //    //var conformance = new CapabilityStatement();

            //Stream s = typeof(CapabilityStatementBuilder).Assembly.GetManifestResourceStream("Spark.Engine.Service.CapabilityStatement.xml");
            //StreamReader sr = new StreamReader(s);
            //string conformanceXml = sr.ReadToEnd();
            
            //var conformance = (CapabilityStatement)FhirParser.ParseResourceFromXml(conformanceXml);

            //conformance.Software = new CapabilityStatement.CapabilityStatementSoftwareComponent();
            //conformance.Software.Version = ReadVersionFromAssembly();
            //conformance.Software.Name = ReadProductNameFromAssembly();
            //conformance.FhirVersion = ModelInfo.Version;
            //conformance.Date = Date.Today().Value;
            //conformance.Meta = new Resource.ResourceMetaComponent();
            //conformance.Meta.VersionId = "0";

            //CapabilityStatement.CapabilityStatementRestComponent serverComponent = conformance.Rest[0];

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

    //        conformance.Text = new Narrative();
    //        conformance.Text.Div = summary.ToString();
    //        return conformance;
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