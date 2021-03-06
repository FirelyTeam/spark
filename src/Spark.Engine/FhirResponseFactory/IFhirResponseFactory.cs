﻿using System;
using System.Collections.Generic;
using Hl7.Fhir.Model;
using Spark.Engine.Core;

namespace Spark.Engine.FhirResponseFactory
{
    public interface IFhirResponseFactory
    {
        FhirResponse GetFhirResponse(Entry entry, IKey key = null, IEnumerable<object> parameters = null);
        FhirResponse GetFhirResponse(Entry entry, IKey key = null, params object[] parameters);
        FhirResponse GetMetadataResponse(Entry entry, IKey key = null);
        FhirResponse GetFhirResponse(IList<Entry> interactions, Bundle.BundleType bundleType);
        FhirResponse GetFhirResponse(Bundle bundle);
        FhirResponse GetFhirResponse(IEnumerable<Tuple<Entry, FhirResponse>> responses, Bundle.BundleType bundleType);
    }
}