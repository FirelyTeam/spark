using Hl7.Fhir.Serialization;
using System;
using System.Diagnostics;
using System.Reflection;

namespace Spark.Engine
{
    public class SparkSettings
    {
        public Uri Endpoint { get; set; }
        public ParserSettings ParserSettings { get; set; }
        public SerializerSettings SerializerSettings { get; set; }
        public ExportSettings ExportSettings { get; set; }
        public string FhirRelease { get; set; }
        public string Version
        {
            get
            {
                var asm = Assembly.GetExecutingAssembly();
                FileVersionInfo version = FileVersionInfo.GetVersionInfo(asm.Location);
                return string.Format("{0}.{1}", version.ProductMajorPart, version.ProductMinorPart);
            }
        }
    }

    public class ExportSettings
    {
        /// <summary>
        /// Whether to externalize FHIR URIs, for example, <code>"Patient"</code> ->
        /// <code>"https://your.fhir.url/fhir/Patient"</code> (<code>false</code> by default).
        /// </summary>
        public bool ExternalizeFhirUri { get; set; }
    }
}
