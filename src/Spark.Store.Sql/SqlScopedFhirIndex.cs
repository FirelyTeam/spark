using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;
using Spark.Search;
using Spark.Store.Sql.Model;
using Spark.Store.Sql.Repository;

namespace Spark.Store.Sql
{
    public class SqlScopedFhirIndex<T> : IScopedFhirIndex<T>
        where T : IScope

    {
        private readonly FhirDbContext context;
        private readonly IFormatId formatId;

        public SqlScopedFhirIndex(IFormatId formatId)
        {
            this.context = new FhirDbContext();
            this.formatId = formatId;
        }

        public void Clean()
        {
            throw new NotImplementedException();
        }

        public void Process(IEnumerable<Entry> entries)
        {
        }

        public void Process(Entry entry)
        {
        }

        public SearchResults Search(string resource, SearchParams searchCommand)
        {
            IEnumerable<int> results =
                context.Resources
                    .Where(r => r.TypeName == resource && r.ScopeKey == Scope.ScopeKey)
                    .Select(r => r.Id);
            SearchResults searchResults = new SearchResults();

            int numMatches = results.Count();

            searchResults.AddRange(results.Select(r => r.ToString()));
            searchResults.MatchCount = numMatches;
            searchResults.UsedCriteria = new List<Criterium>();
            return searchResults;
        }

        public Key FindSingle(string resource, SearchParams searchCommand)
        {
            Resource result = context.Resources
                .Single(r => r.TypeName == resource && r.ScopeKey == Scope.ScopeKey);
            return new Key(String.Empty, result.TypeName, formatId.GetResourceId(result.ResourceId), formatId.GetVersionId(result.VersionId));
        }

        public T Scope { get; set; }
    }
}