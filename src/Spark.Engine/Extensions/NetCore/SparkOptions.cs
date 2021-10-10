// unset

#if NETSTANDARD2_0
using Microsoft.AspNetCore.Mvc;

using System;

namespace Spark.Engine.Extensions
{
    public class SparkOptions
    {
        private SparkSettings _settings = new SparkSettings();
        private readonly FhirServiceExtensionDictionary _fhirExtensionDictionary = new FhirServiceExtensionDictionary();
        private readonly FhirServiceDictionary _fhirServiceDictionary = new FhirServiceDictionary();

        public SparkSettings Settings { get => _settings; set => _settings = value; }
        public FhirServiceExtensionDictionary FhirExtensions { get => _fhirExtensionDictionary; }
        public FhirServiceDictionary FhirServices { get => _fhirServiceDictionary; }
        public Action<MvcOptions> MvcOption { get; set; }
    }
}
#endif