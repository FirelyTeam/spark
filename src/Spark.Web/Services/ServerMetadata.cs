using System.Collections.Generic;
using Hl7.Fhir.Rest;
using Spark.Engine;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Service;
using Spark.Store.Mongo;

namespace Spark.Web.Services
{
	public class ServerMetadata
	{
		private readonly ISearchService _searchService;

		public ServerMetadata(ISearchService searchService)
		{
			_searchService = searchService;
		}

		public List<ResourceStat> GetResourceStats()
		{
			var stats = new List<ResourceStat>();
			List<string> names = Hl7.Fhir.Model.ModelInfo.SupportedResources;

			foreach (string name in names)
			{
				// synchronous for index.cshtml
				var search = _searchService.GetSnapshot(name,new SearchParams { Summary = SummaryType.Count }).GetAwaiter().GetResult(); 
				stats.Add(new ResourceStat() { ResourceName = name, Count = search.Count });
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