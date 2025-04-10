/*
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.AspNetCore.Mvc;
using System;

namespace Spark.Engine.Extensions;

public class SparkOptions
{
    public SparkSettings Settings { get; set; } = new();
    public StoreSettings StoreSettings { get; set; } = new();
    public FhirServiceExtensionDictionary FhirExtensions { get; } = new();

    public FhirServiceDictionary FhirServices { get; } = new();

    public FhirStoreDictionary FhirStores { get; } = new();

    public Action<MvcOptions> MvcOption { get; set; }
}
