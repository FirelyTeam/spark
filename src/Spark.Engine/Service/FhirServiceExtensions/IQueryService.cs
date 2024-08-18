/* 
 * Copyright (c) 2021-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Rest;
using Spark.Engine.Core;
using System.Collections.Generic;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    /// <summary>
    /// Use this interface on your own risk. This interface is highly likely to having breaking changes until
    /// version 2.0 of Spark.
    /// </summary>
    public interface IQueryService : IFhirServiceExtension
    {
        IAsyncEnumerable<Entry> GetAsync(string type, SearchParams searchParams);
    }
}