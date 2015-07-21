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

//using Hl7.Fhir.Support;
using M = MongoDB.Driver.Builders;
using MongoDB.Bson;
using MongoDB.Driver;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;
using Spark.Mongo.Search.Common;

namespace Spark.Search.Mongo
{

    public class MongoSearcher 
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
                query = M.Query.In(join.Field, collector);
                if (join.Resource != null)
                    query = M.Query.And(query, M.Query.EQ(InternalField.RESOURCE, join.Resource));
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
            queries.Add(M.Query.EQ(InternalField.LEVEL, 0)); // geindexeerde contained documents overslaan
            IEnumerable<IMongoQuery> q = parameters.Select(p => ParameterToQuery(p));
            queries.AddRange(q);
            return M.Query.And(queries);
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

        private SearchResults KeysToSearchResults(IEnumerable<BsonValue> keys)
        {
            MongoCursor cursor = collection.Find(M.Query.In(InternalField.ID, keys)).SetFields(InternalField.SELFLINK);

            var results = new SearchResults();
            foreach (BsonDocument document in cursor)
            {
                string id = document.GetValue(InternalField.SELFLINK).ToString();
                //Uri rid = new Uri(id, UriKind.Relative); // NB. these MUST be relative paths. If not, the data at time of input was wrong 
                results.Add(id);
            }
            return results;
        }

        public SearchResults Search(Parameters parameters)
        {
            List<BsonValue> keys = CollectKeys(parameters.WhichFilter);
            int numMatches = keys.Count();
            //RecursiveInclude(parameters.Includes, keys);
            SearchResults results = KeysToSearchResults(keys.Take(parameters.Limit));
            //results.UsedCriteria = parameters.UsedHttpQuery();
            results.MatchCount = numMatches;
            return results;
        }

        private List<BsonValue> CollectKeys(string resourceType, IEnumerable<Criterium> criteria)
        {
            return CollectKeys(resourceType, criteria, null);
        }

        private List<BsonValue> CollectKeys(string resourceType, IEnumerable<Criterium> criteria, SearchResults results)
        {
            //Mapping of original criterium and closed criterium, the former to be able to exclude it if it errors.
            var closedCriteria = new Dictionary<Criterium, Criterium>();
            foreach (var c in criteria)
            {
                if (c.Type == Operator.CHAIN)
                {
                    closedCriteria.Add(c, CloseCriterium(c, resourceType));
                }
                else
                {
                    closedCriteria.Add(c, c);
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
                        criteriaQueries.Add(crit.Value.ToFilter(resourceType));
                    }
                    catch (ArgumentException ex)
                    {
                        if (results == null) throw;
                        results.AddIssue(String.Format("Parameter [{0}] was ignored for the reason: {1}.", crit.Key.ToString(), ex.Message), OperationOutcome.IssueSeverity.Warning);
                        results.UsedCriteria.Remove(crit.Key);
                    }
                }
                if (criteriaQueries.Count > 0)
                {
                    IMongoQuery criteriaQuery = M.Query.And(criteriaQueries);
                    resultQuery = M.Query.And(resultQuery, criteriaQuery);
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
        private Criterium CloseCriterium(Criterium crit, string resourceType)
        {

            List<string> targeted = crit.GetTargetedReferenceTypes(resourceType);
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

        /// <summary>
        /// Change something like Condition/subject:Patient=Patient/10014 
        /// to Condition/subject:Patient.internal_id=Patient/10014, so it is correctly handled as a chained parameter, 
        /// including the filtering on the type in the modifier (if any).
        /// </summary>
        /// <param name="criteria"></param>
        /// <param name="resourceType"></param>
        /// <returns></returns>
        private List<Criterium> NormalizeNonChainedReferenceCriteria(List<Criterium> criteria, string resourceType)
        {
            var result = new List<Criterium>();

            foreach (var crit in criteria)
            {
                var critSp = crit.FindSearchParamDefinition(resourceType);
                if (critSp != null && critSp.Type == Conformance.SearchParamType.Reference && crit.Type != Operator.CHAIN)
                {
                    var subCrit = new Criterium();
                    subCrit.ParamName = InternalField.ID;
                    subCrit.Type = crit.Type;
                    subCrit.Operand = crit.Operand;

                    var superCrit = new Criterium();
                    superCrit.ParamName = crit.ParamName;
                    superCrit.Modifier = crit.Modifier;
                    superCrit.Type = Operator.CHAIN;
                    superCrit.Operand = subCrit;

                    result.Add(superCrit);
                }
                else result.Add(crit);
            }

            return result;
        }

        public SearchResults Search(string resourceType, SearchParams searchCommand)
        {
            SearchResults results = new SearchResults();

            var criteria = parseCriteria(searchCommand, results);

            if (!results.HasErrors)
            {
                results.UsedCriteria = criteria;
                var normalizedCriteria = NormalizeNonChainedReferenceCriteria(criteria, resourceType);
                List<BsonValue> keys = CollectKeys(resourceType, normalizedCriteria, results);

                int numMatches = keys.Count();

                results.AddRange(KeysToSearchResults(keys));
                results.MatchCount = numMatches;
            }

            return results;
        }

        //TODO: Delete, F.Query is obsolete.
        /*
        public SearchResults Search(F.Query query)
        {
            SearchResults results = new SearchResults();

            var criteria = parseCriteria(query, results);

            if (!results.HasErrors)
            {
                results.UsedCriteria = criteria;
                //TODO: ResourceType.ToString() sufficient, or need to use EnumMapping?
                var normalizedCriteria = NormalizeNonChainedReferenceCriteria(criteria, query.ResourceType.ToString());
                List<BsonValue> keys = CollectKeys(query.ResourceType.ToString(), normalizedCriteria, results);

                int numMatches = keys.Count();

                results.AddRange(KeysToSearchResults(keys));
                results.MatchCount = numMatches;
            }

            return results;
        }
        */

        private List<Criterium> parseCriteria(SearchParams searchCommand, SearchResults results)
        {
            var result = new List<Criterium>();
            foreach (var c in searchCommand.Parameters)
            {
                try
                {
                    result.Add(Criterium.Parse(c.Item1, c.Item2));
                }
                catch (Exception ex)
                {
                    results.AddIssue(String.Format("Could not parse parameter [{0}] for reason [{1}].", c.ToString(), ex.Message));
                }
            }
            return result;
        }

        //TODO: Delete, F.Query is obsolete.
        /*
        private List<Criterium> parseCriteria(F.Query query, SearchResults results)
        {
            var result = new List<Criterium>();
            foreach (var c in query.Criteria)
            {
                try
                {
                    result.Add(Criterium.Parse(c));
                }
                catch (Exception ex)
                {
                    results.AddIssue(String.Format("Could not parse parameter [{0}] for reason [{1}].", c.ToString(), ex.Message));
                }
            }
            return result;
        }
         */
    }
}