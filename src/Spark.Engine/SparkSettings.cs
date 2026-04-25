/* 
 * Copyright (c) 2019-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Spark.Engine.Model;
using Spark.Engine.Search;

namespace Spark.Engine;

public class ExperimentalSettings
{
    public IndexingMode IndexingMode { get; set; } = IndexingMode.Synchronous;
}

public class SparkSettings
{
    public Uri Endpoint { get; set; }
    public bool UseAsynchronousIO { get; set; }
    public DeserializerSettings DeserializerSettings { get; set; } = new DeserializerSettings().UsingMode(DeserializationMode.Strict);
    public ExportSettings ExportSettings { get; set; }
    public IndexSettings IndexSettings { get; set; }
    public SearchSettings Search { get; set; }
    public ExperimentalSettings Experimental { get; set; } = new();
    public List<SearchParameter> CustomSearchParameters { get; set; } = [];

    public string Version
    {
        get
        {
            var asm = Assembly.GetExecutingAssembly();
            FileVersionInfo version = FileVersionInfo.GetVersionInfo(asm.Location);
            return $"{version.ProductMajorPart}.{version.ProductMinorPart}.{version.ProductBuildPart}";
        }
    }
}
