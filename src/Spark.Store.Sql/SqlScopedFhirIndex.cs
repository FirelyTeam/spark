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
                    .Where(r => r.ResourceType == resource && r.ScopeKey == Scope.ScopeKey)
                    .Where(r => r.ResourceVersions.Any(v => v.Method == Bundle.HTTPVerb.DELETE.ToString()) == false)
                    .Select( r => new {TypeName = r.ResourceType, r.ResourceId, MaxVersionId = r.ResourceVersions.Max(v => v.InternalVersionId) }).ToList()
                    .Select(r =>  Key.Create(r.TypeName, formatId.GetResourceId(r.ResourceId), r.MaxVersionId.ToString(CultureInfo.InvariantCulture)));

            SearchResults searchResults = new SearchResults();

            int numMatches = results.Count();

            //searchResults.AddRange(results.ToList().Select(r => new Endpoint(String.Empty, resource, r.Id.ToString(), r.InternalVersionId.ToString()).ToString()));
            searchResults.AddRange(results.ToList().Select(key => key.ToString()));
            searchResults.MatchCount = numMatches;
            searchResults.UsedCriteria = new List<Criterium>();
            return searchResults;
        }

        public Key FindSingle(string resource, SearchParams searchCommand)
        {
            Resource result = context.Resources.Include(r=> r.ResourceVersions.OrderByDescending(rv => rv.InternalVersionId).Take(1))
                .Single(r => r.ResourceType == resource && r.ScopeKey == Scope.ScopeKey);
            return new Key(String.Empty, result.ResourceType, formatId.GetResourceId(result.ResourceId), formatId.GetVersionId(result.ResourceVersions.First().InternalVersionId));
        }

        public SearchResults GetReverseIncludes(IList<IKey> keys, IList<string> revIncludes)
        {
            throw new NotImplementedException();
        }


        public IScope Scope { get; set; }
    }
}