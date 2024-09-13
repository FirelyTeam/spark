/*
 * Copyright (c) 2021-2024, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

namespace Spark.Engine;

public class ExportSettings
{
    /// <summary>
    /// Whether to externalize FHIR URIs, for example, <code>"Patient"</code> ->
    /// <code>"https://your.fhir.url/fhir/Patient"</code> (<code>false</code> by default).
    /// </summary>
    public bool ExternalizeFhirUri { get; set; }
}