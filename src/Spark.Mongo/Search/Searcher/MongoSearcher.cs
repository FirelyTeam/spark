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
using Spark.Engine.Extensions;

namespace Spark.Search.Mongo
{

    public class MongoSearcher
    {
        private readonly MongoCollection<BsonDocument> _collection;
        private readonly ILocalhost _localhost;
        private readonly IFhirModel _fhirModel;

        public MongoSearcher(MongoIndexStore mongoIndexStore, ILocalhost localhost, IFhirModel fhirModel)
        {
            _collection = mongoIndexStore.Collection;
            _localhost = localhost;
            _fhirModel = fhirModel;
        }

        private List<BsonValue> CollectKeys(IMongoQuery query)
        {
            MongoCursor<BsonDocument> cursor = _collection.Find(query).SetFields(InternalField.ID);
            return cursor.Select(doc => doc.GetValue(InternalField.ID)).ToList();
        }

        private SearchResults KeysToSearchResults(IEnumerable<BsonValue> keys)
        {
            MongoCursor cursor = _collection.Find(M.Query.In(InternalField.ID, keys)).SetFields(InternalField.SELFLINK);

            var results = new SearchResults();
            foreach (BsonDocument document in cursor)
            {
                string id = document.GetValue(InternalField.SELFLINK).ToString();
                //Uri rid = new Uri(id, UriKind.Relative); // NB. these MUST be relative paths. If not, the data at time of input was wrong 
                results.Add(id);
            }
            return results;
        }

        private List<BsonValue> CollectKeys(string resourceType, IEnumerable<Criterium> criteria, int level = 0)
        {
            return CollectKeys(resourceType, criteria, null, level);
        }

        private List<BsonValue> CollectKeys(string resourceType, IEnumerable<Criterium> criteria, SearchResults results, int level)
        {
            //Mapping of original criterium and closed criterium, the former to be able to exclude it if it errors later on.
            var closedCriteria = new Dictionary<Criterium, Criterium>();
            foreach (var c in criteria)
            {
                if (c.Operator == Operator.CHAIN)
                {
                    try
                    {
                        closedCriteria.Add(c.Clone(), CloseCriterium(c, resourceType, level));
                        //CK: We don't pass the SearchResults on to the (recursive) CloseCriterium. We catch any exceptions only on the highest level.
                    }
                    catch (ArgumentException ex)
                    {
                        if (results == null) throw; //The exception *will* be caught on the highest level.
                        results.AddIssue(String.Format("Parameter [{0}] was ignored for the reason: {1}.", c.ToString(), ex.Message), OperationOutcome.IssueSeverity.Warning);
                        results.UsedCriteria.Remove(c);
                    }
                }
                else
                {
                    //If it is not a chained criterium, we don't need to 'close' it, so it is said to be 'closed' already.
                    closedCriteria.Add(c, c);
                }
            }

            //All chained criteria are 'closed' or 'rolled up' to something like subject IN (id1, id2, id3), so now we AND them with the rest of the criteria.
            IMongoQuery resultQuery = CriteriaMongoExtensions.ResourceFilter(resourceType, level);
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
                        if (results == null) throw; //The exception *will* be caught on the highest level.
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
        /// CloseCriterium("patient.name=\"Teun\"") -> "patient IN (id1,id2)"
        /// </summary>
        /// <param name="resourceType"></param>
        /// <param name="crit"></param>
        /// <returns></returns>
        private Criterium CloseCriterium(Criterium crit, string resourceType, int level)
        {

            List<string> targeted = crit.GetTargetedReferenceTypes(resourceType);
            List<string> allKeys = new List<string>();
            var errors = new List<Exception>();
            foreach (var target in targeted)
            {
                try {
                    Criterium innerCriterium = (Criterium)crit.Operand;
                    var keys = CollectKeys(target, new List<Criterium> { innerCriterium }, ++level);               //Recursive call to CollectKeys!
                    allKeys.AddRange(keys.Select(k => k.ToString()));
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
                }
            if (errors.Count == targeted.Count())
            {
                //It is possible that some of the targets don't support the current parameter. But if none do, there is a serious problem.
                throw new ArgumentException(String.Format("None of the possible target resources support querying for parameter {0}", crit.ParamName));
            }
            crit.Operator = Operator.IN;
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
//                var critSp_ = _fhirModel.FindSearchParameter(resourceType, crit.ParamName); HIER VERDER: kunnen meerdere searchParameters zijn, hoewel dat alleen bij subcriteria van chains het geval is...
                if (critSp != null && critSp.Type == SearchParamType.Reference && crit.Operator != Operator.CHAIN && crit.Modifier != Modifier.MISSING && crit.Operand != null)
                {
                    var subCrit = new Criterium();
                    subCrit.Operator = crit.Operator;
                    string modifier = crit.Modifier;

                    //operand can be one of three things:
                    //1. just the id: 10014 (in the index as internal_justid), the type could be in the modifier
                    //2. full id: Patient/10014 (in the index as internal_id), the type in the modifier is no longer relevant
                    //3. full url: http://localhost:xyz/fhir/Patient/100014, the type in the modifier is also no longer relevant.
                    string operand = (crit.Operand as UntypedValue).Value;
                    if (!operand.Contains("/")) //Situation 1
                    {
                        if (String.IsNullOrWhiteSpace(modifier)) // no modifier, so no info about the referenced type at all
                        {
                            subCrit.ParamName = InternalField.JUSTID;
                            subCrit.Operand = new UntypedValue(operand);
                        }
                        else //modifier contains the referenced type
                        {
                            subCrit.ParamName = InternalField.ID;
                            subCrit.Operand = new UntypedValue(modifier + "/" + operand);
                        }
                    }
                    else //Situation 2 or Situation 3 .
                    {
                        subCrit.ParamName = InternalField.ID;

                        Uri uriOperand;
                        if (Uri.TryCreate(operand, UriKind.RelativeOrAbsolute, out uriOperand)) //Situation 3
                        {
                            var refUri = _localhost.RemoveBase(uriOperand); //Drop the first part if it points to our own server.
                            subCrit.Operand = new UntypedValue(refUri.ToString().TrimStart(new char[] { '/' }));
                        }
                        else
                        {
                            subCrit.Operand = new UntypedValue(operand);
                        }
                    }
                    //subCrit.Operand = crit.Operand;

                    var superCrit = new Criterium();
                    superCrit.ParamName = crit.ParamName;
                    superCrit.Modifier = crit.Modifier;
                    superCrit.Operator = Operator.CHAIN;
                    superCrit.Operand = subCrit;
                    superCrit.SearchParameters.AddRange(crit.SearchParameters);

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
            enrichCriteriaWithSearchParameters(criteria, _fhirModel.GetResourceTypeForResourceName(resourceType));

            if (!results.HasErrors)
            {
                results.UsedCriteria = criteria.Select(c => c.Clone()).ToList();
                var normalizedCriteria = NormalizeNonChainedReferenceCriteria(criteria, resourceType);
                List<BsonValue> keys = CollectKeys(resourceType, normalizedCriteria, results, 0);

                int numMatches = keys.Count();

                results.AddRange(KeysToSearchResults(keys));
                results.MatchCount = numMatches;
            }

            return results;
        }

        private void enrichCriteriumWithSearchParameters(Criterium criterium, ResourceType resourceType)
        {
            var sp = _fhirModel.FindSearchParameter(resourceType, criterium.ParamName);
            var spDef = sp.GetOriginalDefinition();

            if (spDef != null)
            {
                criterium.SearchParameters.Add(spDef);
            }

            if (criterium.Operator == Operator.CHAIN)
            {
                var subCrit = (Criterium)(criterium.Operand);
                foreach (var targetType in criterium.SearchParameters.SelectMany(spd => spd.Target))
                {
                    enrichCriteriumWithSearchParameters(subCrit, targetType);
                }
            }

        }
        private void enrichCriteriaWithSearchParameters(IEnumerable<Criterium> criteria, ResourceType resourceType)
        {
            foreach (var crit in criteria)
            {
                enrichCriteriumWithSearchParameters(crit, resourceType);
            }
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