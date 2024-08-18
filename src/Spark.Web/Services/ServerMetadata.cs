/*
 * Copyright (c) 2019-2024, Incendi (info@incendi.no)
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

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

		public ServerMetadata(ISearchService searchService)
		{
			_searchService = searchService;
		}

		public async Task<List<ResourceStat>> GetResourceStatsAsync()
		{
			var stats = new List<ResourceStat>();
			List<string> names = ModelInfo.SupportedResources;

			foreach (string name in names)
			{
				var search = await _searchService.GetSnapshotAsync(name, new SearchParams { Summary = SummaryType.Count });
				stats.Add(new ResourceStat() { ResourceName = name, Count = search.Count });
			}

			return stats;
		}
	}
}