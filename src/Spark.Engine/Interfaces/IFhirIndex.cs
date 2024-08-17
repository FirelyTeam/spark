/*
 * Copyright (c) 2014-2018, Furore (info@furore.com) and contributors
 * Copyright (c) 2020-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;

namespace Spark.Core
{
    public interface IFhirIndex
    {
        Task CleanAsync();
        Task<SearchResults> SearchAsync(string resource, SearchParams searchCommand);
        Task<Key> FindSingleAsync(string resource, SearchParams searchCommand);
        Task<SearchResults> GetReverseIncludesAsync(IList<IKey> keys, IList<string> revIncludes);
    }
}
