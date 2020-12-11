using System.Collections.Generic;
using Hl7.Fhir.Rest;
using Spark.Engine.Service.FhirServiceExtensions;

namespace Spark.Web.Services
{
    using System.Threading.Tasks;

    public class ServerMetadata
    {
        private readonly ISearchService _searchService;

        public ServerMetadata(ISearchService searchService)
        {
            _searchService = searchService;
        }

        public async Task<List<ResourceStat>> GetResourceStats()
        {
            var stats = new List<ResourceStat>();
            List<string> names = Hl7.Fhir.Model.ModelInfo.SupportedResources;

            foreach (string name in names)
            {
                var search = await _searchService.GetSnapshot(name, new SearchParams { Summary = SummaryType.Count });
                stats.Add(new ResourceStat { ResourceName = name, Count = search.Count });
            }
            return stats;
        }

        public class ResourceStat
        {
            public string ResourceName { get; set; }
            public long Count { get; set; }
        }

        public class ResourceStatsVM
        {
            public List<ResourceStat> ResourceStats;
        }
    }
}