/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using Spark.Service;

namespace Spark.Configuration
{

    public static class Factory
    {

        public static Conformance GetSparkConformance()
        {
            string vsn = Hl7.Fhir.Model.ModelInfo.Version;
            Conformance conformance = ConformanceBuilder.CreateServer("Spark", Info.Version, "Furore", fhirVersion: vsn);

            conformance.AddAllCoreResources(readhistory: true, updatecreate: true, versioning: Conformance.ResourceVersionPolicy.VersionedUpdate);
            conformance.AddAllSystemInteractions().AddAllInteractionsForAllResources().AddCoreSearchParamsAllResources();
            conformance.AddSummaryForAllResources();

            conformance.AcceptUnknown = true;
            conformance.Experimental = true;
            conformance.Format = new string[] { "xml", "json" };
            conformance.Description = "This FHIR SERVER is a reference Implementation server built in C# on HL7.Fhir.Core (nuget) by Furore and others";
            
            return conformance;
        }
       
    }


}