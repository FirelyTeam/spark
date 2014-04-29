/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Hl7.Fhir.Support;
using F = Hl7.Fhir.Model;
using Spark.Core;
using MongoDB.Driver.Builders;
using MongoDB.Bson;
using MongoDB.Driver;
using Hl7.Fhir.Search;

namespace Spark.Search
{
    public class MongoSearcher : ISearcher
    {
        private MongoCollection<BsonDocument> collection;
        public MongoSearcher(MongoCollection<BsonDocument> collection)
        {
            this.collection = collection;
        }

        private IMongoQuery ChainQuery(ChainedParameter parameter)
        {
            IEnumerable<BsonValue> collector = null;

            IMongoQuery query = parameter.Parameter.ToQuery();

            int last = parameter.Joins.Count - 1;
            for (int i = last; i >= 0; i--)
            {
                collector = CollectKeys(query);

                Join join = parameter.Joins[i];
                query = Query.In(join.Field, collector);
                if (join.Resource != null)
                    query = Query.And(query, Query.EQ(InternalField.RESOURCE, join.Resource));
            }
            return query;
        }
        private IMongoQuery ParameterToQuery(IParameter parameter)
        {
            if (parameter is ChainedParameter)
                return ChainQuery(parameter as ChainedParameter);
            else
                return parameter.ToQuery();
        }
        private IMongoQuery ParametersToQuery(IEnumerable<IParameter> parameters)
        {
            List<IMongoQuery> queries = new List<IMongoQuery>();
            queries.Add(Query.EQ(InternalField.LEVEL, 0)); // geindexeerde contained documents overslaan
            IEnumerable<IMongoQuery> q = parameters.Select(p => ParameterToQuery(p));
            queries.AddRange(q);
            return Query.And(queries);
        }

        private List<BsonValue> CollectKeys(IMongoQuery query)
        {
            MongoCursor<BsonDocument> cursor = collection.Find(query).SetFields(InternalField.ID);
            return cursor.Select(doc => doc.GetValue(InternalField.ID)).ToList();
        }
        private List<BsonValue> CollectKeys(IEnumerable<IParameter> parameters)
        {
            var query = ParametersToQuery(parameters);
            List<BsonValue> keys = CollectKeys(query);
            return keys;
        }
        private IEnumerable<BsonValue> CollectForeignKeys(List<BsonValue> keys, string resource, string foreignkey)
        {
            IMongoQuery query = Query.And(Query.EQ(InternalField.RESOURCE, resource), Query.In(InternalField.ID, keys));
            MongoCursor<BsonDocument> cursor = collection.Find(query).SetFields(foreignkey);
            return cursor.Select(doc => doc.GetValue(foreignkey));
        }
        private void Merge(List<BsonValue> keys, IEnumerable<BsonValue> mergekeys)
        {
            IEnumerable<BsonValue> newkeys = mergekeys.Except(keys);
            keys.AddRange(newkeys);
        }
        private void Diverge(IncludeParameter include, List<BsonValue> keys)
        {
            // Dit is een omgekeerde Include: search: organization, params: patient.provider 
            // geeft ook alle patienten vanwie de provider in de geselecteerde organisaties zit. 
            IMongoQuery query = Query.And(Query.EQ(InternalField.RESOURCE, include.TargetResource), Query.In(include.TargetField, keys));
            IEnumerable<BsonValue> output = CollectKeys(query);
            Merge(keys, output);
        }
        private void Include(IncludeParameter include, List<BsonValue> keys)
        {
            IEnumerable<BsonValue> foreignkeys = CollectForeignKeys(keys, include.TargetResource, include.TargetField);
            Merge(keys, foreignkeys);
        }
        private void Include(List<IncludeParameter> includes, List<BsonValue> keys)
        {
            includes.ForEach(include => Include(include, keys));
        }
        private void RecursiveInclude(List<IncludeParameter> includes, List<BsonValue> keys)
        {
            int lastcount, count = 0;
            do
            {
                lastcount = count;
                Include(includes, keys);
                count = keys.Count();
            }
            while (lastcount != count);
        }

        private SearchResults KeysToSearchResults(IEnumerable<BsonValue> keys)
        {
            MongoCursor cursor = collection.Find(Query.In(InternalField.ID, keys)).SetFields(InternalField.SELFLINK);

            var results = new SearchResults();
            foreach (BsonDocument document in cursor)
            {
                string id = document.GetValue(InternalField.SELFLINK).ToString();
                Uri rid = new Uri(id, UriKind.Relative); // NB. these MUST be relative paths. If not, the data at time of input was wrong 
                results.Add(rid);
            }
            return results;
        }

        public SearchResults Search(Parameters parameters)
        {
            List<BsonValue> keys = CollectKeys(parameters.WhichFilter);
            int numMatches = keys.Count();
            RecursiveInclude(parameters.Includes, keys);
            SearchResults results = KeysToSearchResults(keys.Take(parameters.Limit));
            results.UsedParameters = parameters.UsedHttpQuery();
            results.MatchCount = numMatches;
            return results;
        }

        private List<BsonValue> CollectKeys(string resourceType, IEnumerable<Criterium> criteria)
        {
            return CollectKeys(resourceType, criteria, null);
        }

        private List<BsonValue> CollectKeys(string resourceType, IEnumerable<Criterium> criteria, Dictionary<Criterium, string> errors)
        {
            List<Criterium> closedCriteria = new List<Criterium>();
            foreach (var c in criteria)
            {
                if (c.Type == Operator.CHAIN)
                {
                    closedCriteria.Add(CloseCriterium(c));
                }
                else
                {
                    closedCriteria.Add(c);
                }
            }

            IMongoQuery resultQuery = CriteriaMongoExtensions.ResourceFilter(resourceType);
            if (closedCriteria.Count() > 0)
            {
                var criteriaQueries = new List<IMongoQuery>();
                foreach (var crit in closedCriteria)
                {
                    try
                    {
                        criteriaQueries.Add(crit.ToFilter(resourceType));
                    }
                    catch (ArgumentException ex)
                    {
                        if (errors == null) throw;
                        errors.Add(crit, ex.Message);
                    }
                }
                if (criteriaQueries.Count > 0)
                {
                    IMongoQuery criteriaQuery = Query.And(criteriaQueries);
                    resultQuery = Query.And(resultQuery, criteriaQuery);
                }
            }

            return CollectKeys(resultQuery);
        }

        /// <summary>
        /// CloseCriterium("patient.name=\"Teun\"") -> "patient=id1,id2"
        /// </summary>
        /// <param name="resourceType"></param>
        /// <param name="crit"></param>
        /// <returns></returns>
        private Criterium CloseCriterium(Criterium crit)
        {

            List<string> targeted = crit.GetTargetedReferenceTypes();
            List<string> allKeys = new List<string>();
            foreach (var target in targeted)
            {
                var keys = CollectKeys(target, new List<Criterium> { (Criterium)crit.Operand });               //Recursive call to CollectKeys!
                allKeys.AddRange(keys.Select(k => k.ToString()));
            }
            crit.Type = Operator.IN;
            crit.Operand = ChoiceValue.Parse(String.Join(",", allKeys));
            return crit;
        }

        public SearchResults Search(F.Query query)
        {
            var errors = new Dictionary<Criterium, string>();
            var criteria = query.Criteria.Select(c => Criterium.Parse(c)).ToList();
            List<BsonValue> keys = CollectKeys(query.ResourceType, criteria, errors);

            int numMatches = keys.Count();
            SearchResults results = KeysToSearchResults(query.Count.HasValue ? keys.Take(query.Count.Value) : keys);
            results.Errors = errors;
            results.UsedParameters = String.Join("&", criteria.Except(errors.Keys).Select(c => c.ToString()));
            results.MatchCount = numMatches;
            return results;

        }
    }
}