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
using MongoDB.Bson;
using MongoDB.Driver;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Configuration;
using Spark.Engine.Core;
using Spark.Mongo.Search.Common;
using Spark.Engine.Extensions;
using Spark.Engine.Search;
using SM = Spark.Engine.Search.Model;

namespace Spark.Search.Mongo
{

    public class MongoSearcher
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly ILocalhost _localhost;
        private readonly IFhirModel _fhirModel;
        private readonly IReferenceNormalizationService _referenceNormalizationService;

        public MongoSearcher(MongoIndexStore mongoIndexStore, ILocalhost localhost, IFhirModel fhirModel, 
            IReferenceNormalizationService referenceNormalizationService = null)
        {
            _collection = mongoIndexStore.Collection;
            _localhost = localhost;
            _fhirModel = fhirModel;
            _referenceNormalizationService = referenceNormalizationService;
        }

        private List<BsonValue> CollectKeys(FilterDefinition<BsonDocument> query)
        {
            var cursor = _collection.Find(query)
                .Project(Builders<BsonDocument>.Projection.Include(InternalField.ID))
                .ToEnumerable();
            if (cursor.Count() > 0)
                return cursor.Select(doc => doc.GetValue(InternalField.ID)).ToList();
            return new List<BsonValue>();
        }

        private List<BsonValue> CollectSelfLinks(FilterDefinition<BsonDocument> query, SortDefinition<BsonDocument> sortBy)
        {
            var cursor = _collection.Find(query);

            if (sortBy != null)
            {
                cursor.Sort(sortBy);
            }

            cursor = cursor.Project(Builders<BsonDocument>.Projection.Include(InternalField.SELFLINK));

            return cursor.ToEnumerable().Select(doc => doc.GetValue(InternalField.SELFLINK)).ToList();
        }

        private SearchResults KeysToSearchResults(IEnumerable<BsonValue> keys)
        {
            var results = new SearchResults();

            if (keys.Count() > 0)
            {
                var cursor = _collection.Find(Builders<BsonDocument>.Filter.In(InternalField.ID, keys))
                    .Project(Builders<BsonDocument>.Projection.Include(InternalField.SELFLINK))
                    .ToEnumerable();

                foreach (BsonDocument document in cursor)
                {
                    string id = document.GetValue(InternalField.SELFLINK).ToString();
                    //Uri rid = new Uri(id, UriKind.Relative); // NB. these MUST be relative paths. If not, the data at time of input was wrong 
                    results.Add(id);
                }
                results.MatchCount = results.Count();
            }
            return results;
        }

        private List<BsonValue> CollectKeys(string resourceType, IEnumerable<Criterium> criteria, int level = 0)
        {
            return CollectKeys(resourceType, criteria, null, level);
        }

        private List<BsonValue> CollectKeys(string resourceType, IEnumerable<Criterium> criteria, SearchResults results, int level)
        {
            Dictionary<Criterium, Criterium> closedCriteria = CloseChainedCriteria(resourceType, criteria, results, level);

            //All chained criteria are 'closed' or 'rolled up' to something like subject IN (id1, id2, id3), so now we AND them with the rest of the criteria.
            FilterDefinition<BsonDocument> resultQuery = CreateMongoQuery(resourceType, results, level, closedCriteria);

            return CollectKeys(resultQuery);
        }

        private List<BsonValue> CollectSelfLinks(string resourceType, IEnumerable<Criterium> criteria, SearchResults results, int level, IList<Tuple<string, SortOrder>> sortItems )
        {
            Dictionary<Criterium, Criterium> closedCriteria = CloseChainedCriteria(resourceType, criteria, results, level);

            //All chained criteria are 'closed' or 'rolled up' to something like subject IN (id1, id2, id3), so now we AND them with the rest of the criteria.
            FilterDefinition<BsonDocument> resultQuery = CreateMongoQuery(resourceType, results, level, closedCriteria);
            SortDefinition<BsonDocument> sortBy = CreateSortBy(sortItems);
            return CollectSelfLinks(resultQuery, sortBy);
        }

        private static SortDefinition<BsonDocument> CreateSortBy(IList<Tuple<string, SortOrder>> sortItems)
        {
            if (sortItems.Any() == false)
                return null;

            SortDefinition<BsonDocument> sortDefinition = null;
            var first = sortItems.FirstOrDefault();
            if (first.Item2 == SortOrder.Ascending)
            {
                sortDefinition = Builders<BsonDocument>.Sort.Ascending(first.Item1);
            }
            else
            {
                sortDefinition = Builders<BsonDocument>.Sort.Descending(first.Item1);
            }
            sortItems.Remove(first);
            foreach (Tuple<string, SortOrder> sortItem in sortItems)
            {
                if (sortItem.Item2 == SortOrder.Ascending)
                {
                    sortDefinition = sortDefinition.Ascending(sortItem.Item1);
                }
                else
                {
                    sortDefinition = sortDefinition.Descending(sortItem.Item1);
                }
            }
            return sortDefinition;

        }

        private static FilterDefinition<BsonDocument> CreateMongoQuery(string resourceType, SearchResults results, int level, Dictionary<Criterium, Criterium> closedCriteria)
        {
            FilterDefinition<BsonDocument> resultQuery = CriteriaMongoExtensions.ResourceFilter(resourceType, level);
            if (closedCriteria.Count() > 0)
            {
                var criteriaQueries = new List<FilterDefinition<BsonDocument>>();
                foreach (var crit in closedCriteria)
                {
                    if (crit.Value != null)
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
                }
                if (criteriaQueries.Count > 0)
                {
                    FilterDefinition<BsonDocument> criteriaQuery = Builders<BsonDocument>.Filter.And(criteriaQueries);
                    resultQuery = Builders<BsonDocument>.Filter.And(resultQuery, criteriaQuery);
                }
            }

            return resultQuery;
        }

        private Dictionary<Criterium, Criterium> CloseChainedCriteria(string resourceType, IEnumerable<Criterium> criteria, SearchResults results, int level)
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

            return closedCriteria;
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
                try
                {
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
            crit.Operand = new ChoiceValue(allKeys.Select(k => new UntypedValue(k)));
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
        private List<Criterium> NormalizeNonChainedReferenceCriteria(List<Criterium> criteria, string resourceType, SearchSettings searchSettings)
        {
            var result = new List<Criterium>();

            foreach (var crit in criteria)
            {
                var critSp = crit.FindSearchParamDefinition(resourceType);
                //                var critSp_ = _fhirModel.FindSearchParameter(resourceType, crit.ParamName); HIER VERDER: kunnen meerdere searchParameters zijn, hoewel dat alleen bij subcriteria van chains het geval is...
                if (critSp != null && critSp.Type == SearchParamType.Reference && crit.Operator != Operator.CHAIN && crit.Modifier != Modifier.MISSING && crit.Operand != null)
                {
                    if (_referenceNormalizationService != null &&
                        searchSettings.ShouldSkipReferenceCheck(resourceType, crit.ParamName))
                    {
                        var normalizedCriteria = _referenceNormalizationService.GetNormalizedReferenceCriteria(crit);
                        if (normalizedCriteria != null)
                        {
                            result.Add(normalizedCriteria);
                        }
                        continue;
                    }

                    var subCrit = new Criterium();
                    subCrit.Operator = crit.Operator;
                    string modifier = crit.Modifier;

                    //operand can be one of three things:
                    //1. just the id: 10014 (in the index as internal_justid), with no modifier
                    //2. just the id, but with a modifier that contains the type: Patient:10014
                    //3. full id: [http://localhost:xyz/fhir/]Patient/10014 (in the index as internal_id):
                    //  - might start with a host: http://localhost:xyz/fhir/Patient/100014
                    //  - the type in the modifier (if present) is no longer relevant
                    //And above that, you might have multiple identifiers with an IN operator. So we have to cater for that as well.
                    //Because we cannot express an OR construct in Criterium, we have choose one situation for all identifiers. We inspect the first, to determine which situation is appropriate.

                    //step 1: get the operand value, or - in the case of a Choice - the first operand value.
                    string operand = null;
                    if (crit.Operand is ChoiceValue)
                    {
                        ChoiceValue choiceOperand = (crit.Operand as ChoiceValue);
                        if (!choiceOperand.Choices.Any())
                        {
                            continue; //Choice operator without choices: ignore it.
                        }
                        else
                        {
                            operand = (choiceOperand.Choices.First() as UntypedValue).Value;
                        }
                    }
                    else
                    {
                        operand = (crit.Operand as UntypedValue).Value;
                    }

                    //step 2: determine which situation is accurate
                    int situation = 3;
                    if (!operand.Contains("/")) //Situation 1 or 2
                    {
                        if (String.IsNullOrWhiteSpace(modifier)) // no modifier, so no info about the referenced type at all
                        {
                            situation = 1;
                        }
                        else //modifier contains the referenced type
                        {
                            situation = 2;
                        }
                    }

                    //step 3: create a subcriterium appropriate for every situation. 
                    switch (situation)
                    {
                        case 1:
                            subCrit.ParamName = InternalField.JUSTID;
                            subCrit.Operand = crit.Operand;
                            break;
                        case 2:
                            subCrit.ParamName = InternalField.ID;
                            if (crit.Operand is ChoiceValue)
                            {
                                subCrit.Operand = new ChoiceValue(
                                    (crit.Operand as ChoiceValue).Choices.Select(choice =>
                                        new UntypedValue(modifier + "/" + (choice as UntypedValue).Value))
                                        .ToList());
                            }
                            else
                            {
                                subCrit.Operand = new UntypedValue(modifier + "/" + operand);
                            }
                            break;
                        default: //remove the base of the url if there is one and it matches this server
                            subCrit.ParamName = InternalField.ID;
                            if (crit.Operand is ChoiceValue)
                            {
                                subCrit.Operand = new ChoiceValue(
                                    (crit.Operand as ChoiceValue).Choices.Select(choice =>
                                    {
                                        Uri uriOperand;
                                        Uri.TryCreate((choice as UntypedValue).Value, UriKind.RelativeOrAbsolute, out uriOperand);
                                        var refUri = _localhost.RemoveBase(uriOperand); //Drop the first part if it points to our own server.
                                        return new UntypedValue(refUri.ToString().TrimStart(new char[] { '/' }));
                                    }));
                            }
                            else
                            {
                                Uri uriOperand;
                                Uri.TryCreate(operand, UriKind.RelativeOrAbsolute, out uriOperand);
                                var refUri = _localhost.RemoveBase(uriOperand); //Drop the first part if it points to our own server.
                                subCrit.Operand = new UntypedValue(refUri.ToString().TrimStart(new char[] { '/' }));
                            }
                            break;
                    }

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

        public SearchResults Search(string resourceType, SearchParams searchCommand, SearchSettings searchSettings = null)
        {
            if (searchSettings == null)
            {
                searchSettings = new SearchSettings();
            }

            SearchResults results = new SearchResults();

            var criteria = parseCriteria(searchCommand, results);

            if (!results.HasErrors)
            {
                results.UsedCriteria = criteria.Select(c => c.Clone()).ToList();

                criteria = EnrichCriteriaWithSearchParameters(_fhirModel.GetResourceTypeForResourceName(resourceType),
                    results);

                var normalizedCriteria = NormalizeNonChainedReferenceCriteria(criteria, resourceType, searchSettings);
                var normalizeSortCriteria = NormalizeSortItems(resourceType, searchCommand);

                List<BsonValue> selfLinks = CollectSelfLinks(resourceType, normalizedCriteria, results, 0, normalizeSortCriteria);

                foreach (BsonValue selfLink in selfLinks)
                {
                    results.Add(selfLink.ToString());
                }
                results.MatchCount = selfLinks.Count;
            }

            return results;
        }

        private IList<Tuple<string, SortOrder>> NormalizeSortItems(string resourceType, SearchParams searchCommand)
        {
            var sortItems = searchCommand.Sort.Select(s => NormalizeSortItem(resourceType, s)).ToList();
            return sortItems;
        }


        private Tuple<string, SortOrder> NormalizeSortItem(string resourceType, Tuple<string, SortOrder> sortItem)
        {
            ModelInfo.SearchParamDefinition definition =
                _fhirModel.FindSearchParameter(resourceType, sortItem.Item1)?.GetOriginalDefinition();

            if (definition?.Type == SearchParamType.Token)
            {
                return new Tuple<string, SortOrder>(sortItem.Item1 + ".code", sortItem.Item2);
            }
            if (definition?.Type == SearchParamType.Date)
            {
                return new Tuple<string, SortOrder>(sortItem.Item1 + ".start", sortItem.Item2);
            }
            if (definition?.Type == SearchParamType.Quantity)
            {
                return new Tuple<string, SortOrder>(sortItem.Item1 + ".value", sortItem.Item2);
            }
            return sortItem;
        }

        public SearchResults GetReverseIncludes(IList<IKey> keys, IList<string> revIncludes)
        {
            BsonValue[] internal_ids = keys.Select(k => BsonString.Create(String.Format("{0}/{1}", k.TypeName, k.ResourceId))).ToArray();

            SearchResults results = new SearchResults();

            if (keys != null && revIncludes != null)
            {
                var riQueries = new List<FilterDefinition<BsonDocument>>();

                foreach (var revInclude in revIncludes)
                {
                    var ri = SM.ReverseInclude.Parse(revInclude);
                    if (!ri.SearchPath.Contains(".")) //for now, leave out support for chained revIncludes. There aren't that many anyway.
                    {
                        riQueries.Add(
                            Builders<BsonDocument>.Filter.And(
                                Builders<BsonDocument>.Filter.Eq(InternalField.RESOURCE, ri.ResourceType)
                                , Builders<BsonDocument>.Filter.In(ri.SearchPath, internal_ids)));
                    }
                }

                if (riQueries.Count > 0)
                {
                    var revIncludeQuery = Builders<BsonDocument>.Filter.Or(riQueries);
                    var resultKeys = CollectKeys(revIncludeQuery);
                    results = KeysToSearchResults(resultKeys);
                }
            }
            return results;
        }

        private bool TryEnrichCriteriumWithSearchParameters(Criterium criterium, ResourceType resourceType)
        {
            var sp = _fhirModel.FindSearchParameter(resourceType, criterium.ParamName);
            if (sp == null)
            {
                return false;
            }

            var result = true;

            var spDef = sp.GetOriginalDefinition();

            if (spDef != null)
            {
                criterium.SearchParameters.Add(spDef);
            }

            if (criterium.Operator == Operator.CHAIN)
            {
                var subCrit = (Criterium)(criterium.Operand);
                bool subCritResult = false;
                foreach (var targetType in criterium.SearchParameters.SelectMany(spd => spd.Target))
                {
                    //We're ok if at least one of the target types has this searchparameter.
                    subCritResult |= TryEnrichCriteriumWithSearchParameters(subCrit, targetType);
                }
                result &= subCritResult;
            }
            return result;
        }
        private List<Criterium> EnrichCriteriaWithSearchParameters(ResourceType resourceType, SearchResults results)
        {
            var result = new List<Criterium>();
            var notUsed = new List<Criterium>();
            foreach (var crit in results.UsedCriteria)
            {
                if (TryEnrichCriteriumWithSearchParameters(crit, resourceType))
                {
                    result.Add(crit);
                }
                else
                {
                    notUsed.Add(crit);
                    results.AddIssue(String.Format("Parameter with name {0} is not supported for resource type {1}.", crit.ParamName, resourceType), OperationOutcome.IssueSeverity.Warning);
                }
            }

            results.UsedCriteria = results.UsedCriteria.Except(notUsed).ToList();

            return result;
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