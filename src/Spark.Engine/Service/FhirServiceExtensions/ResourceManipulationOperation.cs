﻿using System;
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
        private readonly SearchParams searchCommand;
        public IKey OperationKey { get; }
        public Resource Resource { get; }
        public SearchResults SearchResults { get; }


        private IEnumerable<Entry> interactions;

        protected ResourceManipulationOperation(Resource resource, IKey operationKey, SearchResults searchResults, SearchParams searchCommand = null)
        {
            this.searchCommand = searchCommand;
            this.Resource = resource;
            this.OperationKey = operationKey;
            this.SearchResults = searchResults;
        }

        public IEnumerable<Entry> GetEntries()
        {
            interactions = interactions ?? ComputeEntries();
            return interactions;
        }

        protected abstract IEnumerable<Entry> ComputeEntries();

        protected string GetSearchInformation()
        {
            if (SearchResults == null)
                return null;

            StringBuilder messageBuilder = new StringBuilder();
            messageBuilder.AppendLine();
            if (searchCommand != null)
            {
                string[] parametersNotUsed =
                    searchCommand.Parameters.Where(p => SearchResults.UsedParameters.Contains(p.Item1) == false)
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