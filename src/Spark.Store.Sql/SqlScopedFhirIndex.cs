using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Search;
using Spark.Store.Sql.Model;
using Resource = Spark.Store.Sql.Model.Resource;

namespace Spark.Store.Sql
{
    internal class SqlScopedFhirIndex : IScopedFhirIndex

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
           var results =
                context.Resources
                    .Where(r => r.TypeName == resource && r.ScopeKey == Scope.ScopeKey)
                    .GroupBy(r=>r.ResourceId, e => new  {Id = e.Id,  Method = e.Method})
                    .Where(g=>g.Any(v=>v.Method == Bundle.HTTPVerb.DELETE.ToString()) == false)
                    .Select(g => new  {ResourceId =g.Key, Id = g.Max(r=>r.Id)});
            SearchResults searchResults = new SearchResults();

            int numMatches = results.Count();

            //searchResults.AddRange(results.ToList().Select(r => new Key(String.Empty, resource, r.Id.ToString(), r.VersionId.ToString()).ToString()));
            searchResults.AddRange(results.ToList().Select(r => r.Id.ToString()));
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

     
        public IScope Scope { get; set; }
    }
}