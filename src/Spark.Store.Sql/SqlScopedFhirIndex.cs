using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;
using Spark.Search;
using Spark.Store.Sql.Model;
using System.Data.Entity;
using System.Globalization;
using Resource = Spark.Store.Sql.Model.Resource;

namespace Spark.Store.Sql
{
    internal class SqlScopedFhirIndex : IScopedFhirIndex

    {
        private readonly IFhirDbContext context;
        private readonly IFormatId formatId;

        public SqlScopedFhirIndex(IFormatId formatId, IFhirDbContext dbContext)
        {
            this.context = dbContext;
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
                    .Where(r => r.ResourceVersions.Any(v => v.Method == Bundle.HTTPVerb.DELETE.ToString()) == false)
                    .Select( r => new {r.TypeName, r.ResourceId, MaxVersionId = r.ResourceVersions.Max(v => v.VersionId) }).ToList()
                    .Select(r =>  Key.Create(r.TypeName, formatId.GetResourceId(r.ResourceId), r.MaxVersionId.ToString(CultureInfo.InvariantCulture)));

            SearchResults searchResults = new SearchResults();

            int numMatches = results.Count();

            //searchResults.AddRange(results.ToList().Select(r => new Key(String.Empty, resource, r.Id.ToString(), r.VersionId.ToString()).ToString()));
            searchResults.AddRange(results.ToList().Select(key => key.ToString()));
            searchResults.MatchCount = numMatches;
            searchResults.UsedCriteria = new List<Criterium>();
            return searchResults;
        }

        public Key FindSingle(string resource, SearchParams searchCommand)
        {
            Resource result = context.Resources.Include(r=> r.ResourceVersions.OrderByDescending(rv => rv.VersionId).Take(1))
                .Single(r => r.TypeName == resource && r.ScopeKey == Scope.ScopeKey);
            return new Key(String.Empty, result.TypeName, formatId.GetResourceId(result.ResourceId), formatId.GetVersionId(result.ResourceVersions.First().VersionId));
        }

     
        public IScope Scope { get; set; }
    }
}