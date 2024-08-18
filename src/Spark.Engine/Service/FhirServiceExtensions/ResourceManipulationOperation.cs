/* 
 * Copyright (c) 2016-2018, Firely (info@fire.ly)
 * Copyright (c) 2021-2024, Incendi (info@incendi.no)
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public abstract partial class ResourceManipulationOperation
    {
        private readonly SearchParams _searchCommand;
        private IEnumerable<Entry> _interactions;

        public IKey OperationKey { get; }
        public Resource Resource { get; }
        public SearchResults SearchResults { get; }

        protected ResourceManipulationOperation(Resource resource, IKey operationKey, SearchResults searchResults, SearchParams searchCommand = null)
        {
            _searchCommand = searchCommand;
            Resource = resource;
            OperationKey = operationKey;
            SearchResults = searchResults;
        }

        public IEnumerable<Entry> GetEntries()
        {
            _interactions ??= ComputeEntries();
            return _interactions;
        }

        protected abstract IEnumerable<Entry> ComputeEntries();

        protected string GetSearchInformation()
        {
            if (SearchResults == null)
                return null;

            StringBuilder messageBuilder = new StringBuilder();
            messageBuilder.AppendLine();
            if (_searchCommand != null)
            {
                string[] parametersNotUsed =
                    _searchCommand.Parameters.Where(p => SearchResults.UsedParameters.Contains(p.Item1) == false)
                        .Select(t => t.Item1).ToArray();
                messageBuilder.AppendFormat("Search parameters not used:{0}", string.Join(",", parametersNotUsed));
                messageBuilder.AppendLine();
            }

            messageBuilder.AppendFormat("Search uri used: {0}?{1}", Resource.TypeName, SearchResults.UsedParameters);
            messageBuilder.AppendLine();
            messageBuilder.AppendFormat("Number of matches found: {0}", SearchResults.MatchCount);

            return messageBuilder.ToString();
        }
    }
}