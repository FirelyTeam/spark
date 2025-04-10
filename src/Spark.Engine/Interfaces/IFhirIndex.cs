/*
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2020-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Rest;
using Spark.Engine.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Spark.Engine.Interfaces;

public interface IFhirIndex
{
    Task CleanAsync();
    Task<SearchResults> SearchAsync(string resource, SearchParams searchCommand);
    Task<Key> FindSingleAsync(string resource, SearchParams searchCommand);
    Task<SearchResults> GetReverseIncludesAsync(IList<IKey> keys, IList<string> revIncludes);
}
