using Hl7.Fhir.Serialization;
using System;
using System.Diagnostics;
using System.Reflection;
using Spark.Engine.Search;

namespace Spark.Engine
{
    public class SparkSettings
    {
        public Uri Endpoint { get; set; }
        public ParserSettings ParserSettings { get; set; }
        public SerializerSettings SerializerSettings { get; set; }
        public ExportSettings ExportSettings { get; set; }
        public IndexSettings IndexSettings { get; set; }
        public SearchSettings Search { get; set; }
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

    public class IndexSettings
    {
        /// <summary>
        /// Whether to clear index before rebuilding it. Setting it to <code>false</code> (default)
        /// will may cause stale records to appear in index (for example, when some documents are not
        /// reindexed for some reason).
        /// </summary>
        public bool ClearIndexOnRebuild { get; set; }

        /// <summary>
        /// Number of documents to be loaded into memory for reindexing.
        /// It's recommended to keep it low for having the low memory footprint.
        /// </summary>
        public int ReindexBatchSize { get; set; } = 100;
    }
}
