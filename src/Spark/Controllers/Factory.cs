using Hl7.Fhir.Model;
using Spark.Service;

namespace Spark.Configuration 
{
    public static class Factory
    {

        public static Conformance GetSparkConformance()
        {
            string vsn = Hl7.Fhir.Model.ModelInfo.Version;
            Conformance conformance = ConformanceBuilder.CreateServer("Spark", Settings.Version, "Furore", fhirVersion: vsn);

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