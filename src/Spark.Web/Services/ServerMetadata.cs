using System.Collections.Generic;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Engine.Service.FhirServiceExtensions;

namespace Spark.Web.Services
{
    public partial class ServerMetadata
	{
		private readonly ISearchService _searchService;
		private readonly IAsyncSearchService _asyncSearchService;

		public ServerMetadata(
			ISearchService searchService, 
			IAsyncSearchService asyncSearchService)
		{
			_searchService = searchService;
			_asyncSearchService = asyncSearchService;
		}

		public async Task<List<ResourceStat>> GetResourceStatsAsync()
		{
			var stats = new List<ResourceStat>();
			List<string> names = ModelInfo.SupportedResources;

			foreach (var name in names)
			{
				var search = await _asyncSearchService.GetSnapshotAsync(name, new SearchParams { Summary = SummaryType.Count });
				stats.Add(new ResourceStat() { ResourceName = name, Count = search.Count });
			}

			return stats;
		}
	}
}
