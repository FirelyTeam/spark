/* 
 * Copyright (c) 2016-2018, Furore (info@furore.com) and contributors
 * Copyright (c) 2021-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using System.Threading.Tasks;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface ISearchService : IFhirServiceExtension
    {
        Task<Snapshot> GetSnapshotAsync(string type, SearchParams searchCommand);
        Task<Snapshot> GetSnapshotForEverythingAsync(IKey key);
        Task<IKey> FindSingleAsync(string type, SearchParams searchCommand);
        Task<IKey> FindSingleOrDefaultAsync(string type, SearchParams searchCommand);
        Task<SearchResults> GetSearchResultsAsync(string type, SearchParams searchCommand);
    }
}