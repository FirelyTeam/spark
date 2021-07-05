/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;

namespace Spark.Core
{

    public interface IFhirIndex
    {
        void Clean();
        Task CleanAsync();

        SearchResults Search(string resource, SearchParams searchCommand);
        Task<SearchResults> SearchAsync(string resource, SearchParams searchCommand);

        Key FindSingle(string resource, SearchParams searchCommand);
        Task<Key> FindSingleAsync(string resource, SearchParams searchCommand);

        SearchResults GetReverseIncludes(IList<IKey> keys, IList<string> revIncludes);
        Task<SearchResults> GetReverseIncludesAsync(IList<IKey> keys, IList<string> revIncludes);
    }
}
