/* 
 * Copyright (c) 2016, Furore (info@furore.com) and contributors
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using Hl7.Fhir.Rest;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface ISearchService : IFhirServiceExtension
    {
        Snapshot GetSnapshot(string type, SearchParams searchCommand);

        Snapshot GetSnapshotForEverything(IKey key);

        IKey FindSingle(string type, SearchParams searchCommand);

        IKey FindSingleOrDefault(string type, SearchParams searchCommand);

        SearchResults GetSearchResults(string type, SearchParams searchCommand);
    }
}
