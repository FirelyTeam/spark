/* 
 * Copyright (c) 2021-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

#if NETSTANDARD2_0 || NET6_0
using Microsoft.AspNetCore.Mvc;
using System;

namespace Spark.Engine.Extensions
{
    public class SparkOptions
    {
        private SparkSettings _settings = new SparkSettings();
        private StoreSettings _storeSettings = new StoreSettings();
        private readonly FhirServiceExtensionDictionary _fhirExtensionDictionary = new FhirServiceExtensionDictionary();
        private readonly FhirServiceDictionary _fhirServiceDictionary = new FhirServiceDictionary();
        private readonly FhirStoreDictionary _fhirStoreDictionary = new FhirStoreDictionary();

        public SparkSettings Settings { get => _settings; set => _settings = value; }
        public StoreSettings StoreSettings { get => _storeSettings; set => _storeSettings = value; }
        public FhirServiceExtensionDictionary FhirExtensions { get => _fhirExtensionDictionary; }
        public FhirServiceDictionary FhirServices { get => _fhirServiceDictionary; }
        public FhirStoreDictionary FhirStores { get => _fhirStoreDictionary; }
        public Action<MvcOptions> MvcOption { get; set; }
    }
}
#endif