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
using System.Web;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Spark.Service;
using Spark.Config;
using Spark.Data.AmazonS3;
using System.Configuration;
using Spark.Core;
using Spark.Store;

namespace Spark.Support
{
    
    public static class Factory
    {
        static MongoStoreFactory storefactory = new MongoStoreFactory(Settings.MongoUrl);
        static volatile ILocalhost localhost = new SingleLocalhost(Settings.Endpoint);

        public static FhirService GetMongoFhirService()
        {
            return storefactory.MongoFhirService(localhost);
        }

        public static FhirMaintenanceService GetFhirMaintenanceService()
        {
            FhirService service = DependencyCoupler.Inject<FhirService>();
            return new FhirMaintenanceService(service);
        }

        public static Conformance GetSparkConformance()
        {
            Conformance conformance = ConformanceBuilder.CreateServer("Spark", Info.Version, "Furore", fhirVersion: "0.4.0");
            
            conformance.AddServer();
            conformance.AddAllCoreResources(readhistory: true, updatecreate: true, versioning: Conformance.ResourceVersionPolicy.VersionedUpdate);
            conformance.AddAllSystemInteractions().AddAllInteractionsForAllResources().AddCoreSearchParamsAllResources();

            conformance.AcceptUnknown = true;
            conformance.Experimental = true;
            conformance.Format = new string[] { "xml", "json" };
            conformance.Description = "This FHIR SERVER is a reference Implementation server built in C# on HL7.Fhir.Core (nuget) by Furore and others";
            

            return conformance;
        }
       
    }


}