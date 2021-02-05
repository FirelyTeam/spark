/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
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
        [Obsolete("Use Async method version instead")]
        void Clean();

        [Obsolete("Use Async method version instead")]
        SearchResults Search(string resource, SearchParams searchCommand);

        [Obsolete("Use Async method version instead")]
        Key FindSingle(string resource, SearchParams searchCommand);

        [Obsolete("Use Async method version instead")]
        SearchResults GetReverseIncludes(IList<IKey> keys, IList<string> revIncludes);

        Task CleanAsync();

        Task<SearchResults> SearchAsync(string resource, SearchParams searchCommand);

        Task<Key> FindSingleAsync(string resource, SearchParams searchCommand);

        Task<SearchResults> GetReverseIncludesAsync(IList<IKey> keys, IList<string> revIncludes);
    }
}
