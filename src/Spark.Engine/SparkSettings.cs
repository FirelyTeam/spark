﻿using Hl7.Fhir.Serialization;
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
}
