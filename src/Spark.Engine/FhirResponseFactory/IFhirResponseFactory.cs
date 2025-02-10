/*
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * Copyright (c) 2019-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Spark.Engine.Core;
using System;
using System.Collections.Generic;

namespace Spark.Engine.FhirResponseFactory;

public interface IFhirResponseFactory
{
    FhirResponse<T> GetFhirResponse<T>(Entry entry, IKey key = null, IEnumerable<object> parameters = null)
        where T : Resource;
    FhirResponse GetFhirResponse(Entry entry, IKey key = null, IEnumerable<object> parameters = null);
    FhirResponse<T> GetFhirResponse<T>(Entry entry, IKey key = null, params object[] parameters)
        where T : Resource;
    FhirResponse GetFhirResponse(Entry entry, IKey key = null, params object[] parameters);
    FhirResponse GetMetadataResponse(Entry entry, IKey key = null);
    FhirResponse GetFhirResponse(IList<Entry> interactions, Bundle.BundleType bundleType);
    FhirResponse GetFhirResponse(Bundle bundle);
    FhirResponse GetFhirResponse(IEnumerable<Tuple<Entry, FhirResponse>> responses, Bundle.BundleType bundleType);
}
