/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Engine.Core;

namespace Spark.Engine.Utility;

// FIXME: Currently a hack for our extension methods and static methods to retrieve a reference to the DI
//        injected IFhirModel.
internal static class StaticReferenceToFhirModel
{
    public static void Initialize(IFhirModel fhirModel)
    {
        FhirModel =  fhirModel;
    }

    public static IFhirModel FhirModel { get; private set; }
}
