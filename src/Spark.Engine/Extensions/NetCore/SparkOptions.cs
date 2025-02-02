/* 
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

#if NETSTANDARD2_1 || NET6_0_OR_GREATER
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
