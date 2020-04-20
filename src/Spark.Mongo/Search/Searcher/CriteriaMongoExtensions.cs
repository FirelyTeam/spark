/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using System.Text.RegularExpressions;
using System.Reflection;
using Spark.Mongo.Search.Common;
using Spark.Engine.Extensions;
using Hl7.Fhir.Utility;

namespace Spark.Search.Mongo
{

    // todo: DSTU2 - NonExistent classes: Operator, Expression, ValueExpression

    internal static class CriteriaMongoExtensions
    {
        private static List<MethodInfo> FixedQueries = CacheQueryMethods();

        private static List<MethodInfo> CacheQueryMethods()
        {
            return typeof(CriteriaMongoExtensions).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Where(m => m.Name.EndsWith("FixedQuery")).ToList();
        }

        //internal static FilterDefinition<BsonDocument> ResourceFilter(this Query query)
        //{
        //    return ResourceFilter(query.ResourceType);
        //}

        internal static FilterDefinition<BsonDocument> ResourceFilter(string resourceType, int level)
        {
            var queries = new List<FilterDefinition<BsonDocument>>();
            if (level == 0)
                queries.Add(Builders<BsonDocument>.Filter.Eq(InternalField.LEVEL, 0));
            queries.Add(Builders<BsonDocument>.Filter.Eq(InternalField.RESOURCE, resourceType));

            return Builders<BsonDocument>.Filter.And(queries);
        }

        internal static ModelInfo.SearchParamDefinition FindSearchParamDefinition(this Criterium param, string resourceType)
        {
            return param.SearchParameters?.FirstOrDefault(sp => sp.Resource == resourceType || sp.Resource == "Resource");

            //var sp = ModelInfo.SearchParameters;
            //return sp.Find(defn => defn.Name == param.ParamName && defn.Resource == resourceType);
        }

        internal static FilterDefinition<BsonDocument> ToFilter(this Criterium param, string resourceType)
        {
            //Maybe it's a generic parameter.
            MethodInfo methodForParameter = FixedQueries.Find(m => m.Name.Equals(param.ParamName + "FixedQuery"));
            if (methodForParameter != null)
            {
                return (FilterDefinition<BsonDocument>)methodForParameter.Invoke(null, new object[] { param });
            }

            //Otherwise it should be a parameter as defined in the metadata
            var critSp = FindSearchParamDefinition(param, resourceType);
            if (critSp != null)
            {

                // todo: DSTU2 - modifier not in SearchParameter
                return CreateFilter(critSp, param.Operator, param.Modifier, param.Operand);
                //return null;
            }

            throw new ArgumentException(String.Format("Resource {0} has no parameter with the name {1}.", resourceType, param.ParamName));
        }

        private static FilterDefinition<BsonDocument> SetParameter(this FilterDefinition<BsonDocument> query, string parameterName, IEnumerable<String> values)
        {
            return query.SetParameter(new BsonArray() { parameterName }.ToJson(), new BsonArray(values).ToJson());
        }

        private static FilterDefinition<BsonDocument> SetParameter(this FilterDefinition<BsonDocument> query, string parameterName, String value)
        {
            return BsonDocument.Parse(query.ToString().Replace(parameterName, value));
        }

        private static FilterDefinition<BsonDocument> CreateFilter(ModelInfo.SearchParamDefinition parameter, Operator op, String modifier, Expression operand)
        {
            if (op == Operator.CHAIN)
            {
                throw new NotSupportedException("Chain operators should be handled in MongoSearcher.");
            }
            else // There's only one operand.
            {
                string parameterName = parameter.Name;
                if (parameterName == "_id")
                    parameterName = "fhir_id"; //See MongoIndexMapper for counterpart.

                var valueOperand = (ValueExpression)operand;
                switch (parameter.Type)
                {
                    case SearchParamType.Composite:
                        return CompositeQuery(parameter, op, modifier, valueOperand);
                    case SearchParamType.Date:
                        return DateQuery(parameterName, op, modifier, valueOperand);
                    case SearchParamType.Number:
                        return NumberQuery(parameter.Name, op, valueOperand);
                    case SearchParamType.Quantity:
                        return QuantityQuery(parameterName, op, modifier, valueOperand);
                    case SearchParamType.Reference:
                        //Chain is handled in MongoSearcher, so here we have the result of a closed criterium: IN [ list of id's ]
                        return StringQuery(parameterName, op, modifier, valueOperand);
                    case SearchParamType.String:
                        return StringQuery(parameterName, op, modifier, valueOperand);
                    case SearchParamType.Token:
                        return TokenQuery(parameterName, op, modifier, valueOperand);
                    case SearchParamType.Uri:
                        return UriQuery(parameterName, op, modifier, valueOperand);
                    default:
                        //return Builders<BsonDocument>.Filter.Null;
                        throw new NotSupportedException(String.Format("SearchParamType {0} on parameter {1} not supported.", parameter.Type, parameter.Name));
                }
            }
        }

        private static List<string> GetTargetedReferenceTypes(ModelInfo.SearchParamDefinition parameter, String modifier)
        {
            var allowedResourceTypes = parameter.Target.Select(t => EnumUtility.GetLiteral(t)).ToList();// ModelInfo.SupportedResources; //TODO: restrict to parameter.ReferencedResources. This means not making this static, because you want to use IFhirModel.
            List<string> searchResourceTypes = new List<string>();
            if (String.IsNullOrEmpty(modifier))
                searchResourceTypes.AddRange(allowedResourceTypes);
            else if (allowedResourceTypes.Contains(modifier))
            {
                searchResourceTypes.Add(modifier);
            }
            else
            {
                throw new NotSupportedException(String.Format("Referenced type cannot be of type %s.", modifier));
            }

            return searchResourceTypes;
        }

        internal static List<string> GetTargetedReferenceTypes(this Criterium chainCriterium, string resourceType)
        {

            if (chainCriterium.Operator != Operator.CHAIN)
                throw new ArgumentException("Targeted reference types are only relevent for chained criteria.");

            var critSp = chainCriterium.FindSearchParamDefinition(resourceType);
            var modifier = chainCriterium.Modifier;
            var nextInChain = (Criterium)chainCriterium.Operand;
            var nextParameter = nextInChain.ParamName;
            // The modifier contains the type of resource that the referenced resource must be. It is optional.
            // If not present, search all possible types of resources allowed at this reference.
            // If it is present, it should be of one of the possible types.

            var searchResourceTypes = GetTargetedReferenceTypes(critSp, modifier);

            // Afterwards, filter on the types that actually have the requested searchparameter.
            return searchResourceTypes.Where(rt => InternalField.All.Contains(nextParameter) || UniversalField.All.Contains(nextParameter) || ModelInfo.SearchParameters.Exists(sp => rt.Equals(sp.Resource) && nextParameter.Equals(sp.Name))).ToList();
        }

        private static FilterDefinition<BsonDocument> StringQuery(String parameterName, Operator optor, String modifier, ValueExpression operand)
        {
            switch (optor)
            {
                case Operator.EQ:
                    var typedOperand = ((UntypedValue)operand).AsStringValue().ToString();
                    switch (modifier)
                    {
                        case Modifier.EXACT:
                            return Builders<BsonDocument>.Filter.Eq(parameterName, typedOperand);
                        case Modifier.CONTAINS:
                            return Builders<BsonDocument>.Filter.Regex(parameterName, new BsonRegularExpression(new Regex($".*{typedOperand}.*", RegexOptions.IgnoreCase)));
                        case Modifier.TEXT: //the same behaviour as :phonetic in previous versions.
                            return Builders<BsonDocument>.Filter.Regex(parameterName + "soundex", "^" + typedOperand);
                        //case Modifier.BELOW:
                        //    return Builders<BsonDocument>.Filter.Matches(parameterName, typedOperand + ".*")
                        case Modifier.NONE:
                        case null:
                            //partial from begin
                            return Builders<BsonDocument>.Filter.Regex(parameterName, new BsonRegularExpression("^" + typedOperand, "i"));
                        default:
                            throw new ArgumentException(String.Format("Invalid modifier {0} on string parameter {1}", modifier, parameterName));
                    }
                case Operator.IN: //We'll only handle choice like :exact
                    IEnumerable<ValueExpression> opMultiple = ((ChoiceValue)operand).Choices;
                    return SafeIn(parameterName, new BsonArray(opMultiple.Cast<UntypedValue>().Select(sv => sv.Value)));
                case Operator.ISNULL:
                    return Builders<BsonDocument>.Filter.Or(Builders<BsonDocument>.Filter.Exists(parameterName, false), Builders<BsonDocument>.Filter.Eq(parameterName, BsonNull.Value)); //With only Builders<BsonDocument>.Filter.NotExists, that would exclude resources that have this field with an explicit null in it.
                case Operator.NOTNULL:
                    return Builders<BsonDocument>.Filter.Ne(parameterName, BsonNull.Value); //We don't use Builders<BsonDocument>.Filter.Exists, because that would include resources that have this field with an explicit null in it.
                default:
                    throw new ArgumentException(String.Format("Invalid operator {0} on string parameter {1}", optor.ToString(), parameterName));
            }
        }

        //No modifiers allowed on number parameters, hence not in the method signature.
        private static FilterDefinition<BsonDocument> NumberQuery(String parameterName, Operator optor, ValueExpression operand)
        {
            string typedOperand;
            try
            {
                typedOperand = ((UntypedValue)operand).AsNumberValue().ToString();
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException(String.Format("Invalid number value {0} on number parameter {1}", operand, parameterName));
            }
            catch (FormatException)
            {
                throw new ArgumentException(String.Format("Invalid number value {0} on number parameter {1}", operand, parameterName));
            }

            switch (optor)
            {
                case Operator.APPROX:
                //TODO
                case Operator.CHAIN:
                //Invalid in this context
                case Operator.EQ:
                    return Builders<BsonDocument>.Filter.Eq(parameterName, typedOperand);
                case Operator.GT:
                    return Builders<BsonDocument>.Filter.Gt(parameterName, typedOperand);
                case Operator.GTE:
                    return Builders<BsonDocument>.Filter.Gte(parameterName, typedOperand);
                case Operator.IN:
                    IEnumerable<ValueExpression> opMultiple = ((ChoiceValue)operand).Choices;
                    return SafeIn(parameterName, new BsonArray(opMultiple.Cast<NumberValue>().Select(nv => nv.Value)));
                case Operator.ISNULL:
                    return Builders<BsonDocument>.Filter.Eq(parameterName, BsonNull.Value);
                case Operator.LT:
                    return Builders<BsonDocument>.Filter.Lt(parameterName, typedOperand);
                case Operator.LTE:
                    return Builders<BsonDocument>.Filter.Lte(parameterName, typedOperand);
                case Operator.NOTNULL:
                    return Builders<BsonDocument>.Filter.Ne(parameterName, BsonNull.Value);
                default:
                    throw new ArgumentException(String.Format("Invalid operator {0} on number parameter {1}", optor.ToString(), parameterName));
            }
        }

        private static FilterDefinition<BsonDocument> ExpressionQuery(string name, Operator optor, BsonValue value)
        {
            switch (optor)
            {
                case Operator.EQ:
                    return Builders<BsonDocument>.Filter.Eq(name, value);

                case Operator.GT:
                    return Builders<BsonDocument>.Filter.Gt(name, value);

                case Operator.GTE:
                    return Builders<BsonDocument>.Filter.Gte(name, value);

                case Operator.ISNULL:
                    return Builders<BsonDocument>.Filter.Eq(name, BsonNull.Value);

                case Operator.LT:
                    return Builders<BsonDocument>.Filter.Lt(name, value);

                case Operator.LTE:
                    return Builders<BsonDocument>.Filter.Lte(name, value);

                case Operator.NOTNULL:
                    return Builders<BsonDocument>.Filter.Ne(name, BsonNull.Value);

                default:
                    throw new ArgumentException(String.Format("Invalid operator {0} on token parameter {1}", optor.ToString(), name));
            }
        }

        private static FilterDefinition<BsonDocument> QuantityQuery(String parameterName, Operator optor, String modifier, ValueExpression operand)
        {
            //$elemMatch only works on array values. But the MongoIndexMapper only creates an array if there are multiple values for a given parameter.
            //So we also construct a query for when there is only one set of values in the searchIndex, hence there is no array.
            var quantity = operand.ToModelQuantity();
            Fhir.Metrics.Quantity q = quantity.ToUnitsOfMeasureQuantity().Canonical();
            string decimals = q.SearchableString();
            BsonValue value = q.GetValueAsBson();

            List<FilterDefinition<BsonDocument>> arrayQueries = new List<FilterDefinition<BsonDocument>>();
            List<FilterDefinition<BsonDocument>> noArrayQueries = new List<FilterDefinition<BsonDocument>>() { Builders<BsonDocument>.Filter.Not(Builders<BsonDocument>.Filter.Type(parameterName, BsonType.Array)) };
            switch (optor)
            {
                case Operator.EQ:
                    arrayQueries.Add(Builders<BsonDocument>.Filter.Regex("decimals", new BsonRegularExpression("^" + decimals)));
                    noArrayQueries.Add(Builders<BsonDocument>.Filter.Regex(parameterName + ".decimals", new BsonRegularExpression("^" + decimals)));
                    break;

                default:
                    arrayQueries.Add(ExpressionQuery("value", optor, value));
                    noArrayQueries.Add(ExpressionQuery(parameterName + ".value", optor, value));
                    break;
            }

            if (quantity.System != null)
            {
                arrayQueries.Add(Builders<BsonDocument>.Filter.Eq("system", quantity.System.ToString()));
                noArrayQueries.Add(Builders<BsonDocument>.Filter.Eq(parameterName + ".system", quantity.System.ToString()));
            }
            arrayQueries.Add(Builders<BsonDocument>.Filter.Eq("unit", q.Metric.ToString()));
            noArrayQueries.Add(Builders<BsonDocument>.Filter.Eq(parameterName + ".unit", q.Metric.ToString()));

            var arrayQuery = Builders<BsonDocument>.Filter.ElemMatch(parameterName, Builders<BsonDocument>.Filter.And(arrayQueries));
            var noArrayQuery = Builders<BsonDocument>.Filter.And(noArrayQueries);

            FilterDefinition<BsonDocument> query = Builders<BsonDocument>.Filter.Or(arrayQuery, noArrayQuery);
            return query;
        }

        private static FilterDefinition<BsonDocument> TokenQuery(String parameterName, Operator optor, String modifier, ValueExpression operand)
        {
            //$elemMatch only works on array values. But the MongoIndexMapper only creates an array if there are multiple values for a given parameter.
            //So we also construct a query for when there is only one set of values in the searchIndex, hence there is no array.
            string systemfield = parameterName + ".system";
            string codefield = parameterName + ".code";
            string textfield = parameterName + ".text";

            switch (optor)
            {
                case Operator.EQ:
                    var typedEqOperand = ((UntypedValue)operand).AsTokenValue();
                    if (modifier == Modifier.TEXT)
                    {
                        return Builders<BsonDocument>.Filter.Regex(textfield, new BsonRegularExpression(typedEqOperand.Value, "i"));
                    }
                    else //Search on code and system
                    {
                        //Set up two variants of queries, for dealing with single token values in the index, and multiple (in an array).
                        var arrayQueries = new List<FilterDefinition<BsonDocument>>();
                        var noArrayQueries = new List<FilterDefinition<BsonDocument>>(){
                            Builders<BsonDocument>.Filter.Not(Builders<BsonDocument>.Filter.Type(parameterName, BsonType.Array))};

                        if (modifier == Modifier.NOT) //NOT modifier only affects matching the code, not the system
                        {
                            noArrayQueries.Add(Builders<BsonDocument>.Filter.Exists(parameterName));
                            noArrayQueries.Add(Builders<BsonDocument>.Filter.Ne(codefield, typedEqOperand.Value));
                            arrayQueries.Add(Builders<BsonDocument>.Filter.Exists(parameterName));
                            arrayQueries.Add(Builders<BsonDocument>.Filter.Ne("code", typedEqOperand.Value));
                        }
                        else
                        {
                            noArrayQueries.Add(Builders<BsonDocument>.Filter.Eq(codefield, typedEqOperand.Value));
                            arrayQueries.Add(Builders<BsonDocument>.Filter.Eq("code", typedEqOperand.Value));
                        }

                        //Handle the system part, if present.
                        if (!typedEqOperand.AnyNamespace)
                        {
                            if (String.IsNullOrWhiteSpace(typedEqOperand.Namespace))
                            {
                                arrayQueries.Add(Builders<BsonDocument>.Filter.Exists("system", false));
                                noArrayQueries.Add(Builders<BsonDocument>.Filter.Exists(systemfield, false));
                            }
                            else
                            {
                                arrayQueries.Add(Builders<BsonDocument>.Filter.Eq("system", typedEqOperand.Namespace));
                                noArrayQueries.Add(Builders<BsonDocument>.Filter.Eq(systemfield, typedEqOperand.Namespace));
                            }
                        }

                        //Combine code and system
                        var arrayEqQuery = Builders<BsonDocument>.Filter.ElemMatch(parameterName, Builders<BsonDocument>.Filter.And(arrayQueries));
                        var noArrayEqQuery = Builders<BsonDocument>.Filter.And(noArrayQueries);
                        return Builders<BsonDocument>.Filter.Or(arrayEqQuery, noArrayEqQuery);
                    }
                case Operator.IN:
                    IEnumerable<ValueExpression> opMultiple = ((ChoiceValue)operand).Choices;
                    return Builders<BsonDocument>.Filter.Or(opMultiple.Select(choice => TokenQuery(parameterName, Operator.EQ, modifier, choice)));
                case Operator.ISNULL:
                    return Builders<BsonDocument>.Filter.And(Builders<BsonDocument>.Filter.Eq(parameterName, BsonNull.Value), Builders<BsonDocument>.Filter.Eq(textfield, BsonNull.Value)); //We don't use Builders<BsonDocument>.Filter.NotExists, because that would exclude resources that have this field with an explicit null in it.
                case Operator.NOTNULL:
                    return Builders<BsonDocument>.Filter.Or(Builders<BsonDocument>.Filter.Ne(parameterName, BsonNull.Value), Builders<BsonDocument>.Filter.Eq(textfield, BsonNull.Value)); //We don't use Builders<BsonDocument>.Filter.Exists, because that would include resources that have this field with an explicit null in it.
                default:
                    throw new ArgumentException(String.Format("Invalid operator {0} on token parameter {1}", optor.ToString(), parameterName));
            }
        }

        private static FilterDefinition<BsonDocument> UriQuery(String parameterName, Operator optor, String modifier, ValueExpression operand)
        {
            //CK: Ugly implementation by just using existing features on the StringQuery.
            //TODO: Implement :ABOVE.
            String localModifier = "";
            switch (modifier)
            {
                case Modifier.BELOW:
                    //Without a modifier the default string search is left partial, which is what we need for Uri:below :-)
                    break;
                case Modifier.ABOVE:
                    //Not supported by string search, still TODO.
                    throw new NotImplementedException(String.Format("Modifier {0} on Uri parameter {1} not supported yet.", modifier, parameterName));
                case Modifier.NONE:
                case null:
                    localModifier = Modifier.EXACT;
                    break;
                case Modifier.MISSING:
                    localModifier = Modifier.MISSING;
                    break;
                default:
                    throw new ArgumentException(String.Format("Invalid modifier {0} on Uri parameter {1}", modifier, parameterName));
            }
            return StringQuery(parameterName, optor, localModifier, operand);
        }

        private static string GroomDate(string value)
        {
            if (value != null)
            {
                string s = Regex.Replace(value, @"[T\s:\-]", "");
                int i = s.IndexOf('+');
                if (i > 0) s = s.Remove(i);
                return s;
            }
            else
                return null;
        }

        private static FilterDefinition<BsonDocument> DateQuery(String parameterName, Operator optor, String modifier, ValueExpression operand)
        {
            if (optor == Operator.IN)
            {
                IEnumerable<ValueExpression> opMultiple = ((ChoiceValue)operand).Choices;
                return Builders<BsonDocument>.Filter.Or(opMultiple.Select(choice => DateQuery(parameterName, Operator.EQ, modifier, choice)));
            }

            string start = parameterName + ".start";
            string end = parameterName + ".end";

            var fdtValue = ((UntypedValue)operand).AsDateTimeValue();
            var valueLower = BsonDateTime.Create(fdtValue.LowerBound());
            var valueUpper = BsonDateTime.Create(fdtValue.UpperBound());

            switch (optor)
            {
                case Operator.EQ:
                    return
                        Builders<BsonDocument>.Filter.And(Builders<BsonDocument>.Filter.Gte(end, valueLower), Builders<BsonDocument>.Filter.Lt(start, valueUpper));
                case Operator.GT:
                    return
                        Builders<BsonDocument>.Filter.Gte(start, valueUpper);
                case Operator.GTE:
                    return
                        Builders<BsonDocument>.Filter.Gte(start, valueLower);
                case Operator.LT:
                    return
                        Builders<BsonDocument>.Filter.Lt(end, valueLower);
                case Operator.LTE:
                    return
                        Builders<BsonDocument>.Filter.Lt(end, valueUpper);
                case Operator.ISNULL:
                    return Builders<BsonDocument>.Filter.Eq(parameterName, BsonNull.Value); //We don't use Builders<BsonDocument>.Filter.NotExists, because that would exclude resources that have this field with an explicit null in it.
                case Operator.NOTNULL:
                    return Builders<BsonDocument>.Filter.Ne(parameterName, BsonNull.Value); //We don't use Builders<BsonDocument>.Filter.Exists, because that would include resources that have this field with an explicit null in it.
                default:
                    throw new ArgumentException(String.Format("Invalid operator {0} on date parameter {1}", optor.ToString(), parameterName));
            }
        }

        private static FilterDefinition<BsonDocument> CompositeQuery(ModelInfo.SearchParamDefinition parameterDef, Operator optor, String modifier, ValueExpression operand)
        {
            if (optor == Operator.IN)
            {
                var choices = ((ChoiceValue)operand);
                var queries = new List<FilterDefinition<BsonDocument>>();
                foreach (var choice in choices.Choices)
                {
                    queries.Add(CompositeQuery(parameterDef, Operator.EQ, modifier, choice));
                }
                return Builders<BsonDocument>.Filter.Or(queries);
            }
            else if (optor == Operator.EQ)
            {
                var typedOperand = (CompositeValue)operand;
                var queries = new List<FilterDefinition<BsonDocument>>();
                var components = typedOperand.Components;
                var subParams = parameterDef.CompositeParams;

                if (components.Count() != subParams.Count())
                {
                    throw new ArgumentException(String.Format("Parameter {0} requires exactly {1} composite values, not the currently provided {2} values.", parameterDef.Name, subParams.Count(), components.Count()));
                }

                for (int i = 0; i < subParams.Count(); i++)
                {
                    var subCrit = new Criterium();
                    subCrit.Operator = Operator.EQ;
                    subCrit.ParamName = subParams[i];
                    subCrit.Operand = components[i];
                    subCrit.Modifier = modifier;
                    queries.Add(subCrit.ToFilter(parameterDef.Resource));
                }
                return Builders<BsonDocument>.Filter.And(queries);
            }
            throw new ArgumentException(String.Format("Invalid operator {0} on composite parameter {1}", optor.ToString(), parameterDef.Name));
        }

        //internal static FilterDefinition<BsonDocument> _lastUpdatedFixedQuery(Criterium crit)
        //{
        //    if (crit.Operator == Operator.IN)
        //    {
        //        IEnumerable<ValueExpression> opMultiple = ((ChoiceValue)crit.Operand).Choices;
        //        IEnumerable<Criterium> criteria = opMultiple.Select<ValueExpression, Criterium>(choice => new Criterium() { ParamName = crit.ParamName, Modifier = crit.Modifier, Operator = Operator.EQ, Operand = choice });
        //        return Builders<BsonDocument>.Filter.Or(criteria.Select(criterium => _lastUpdatedFixedQuery(criterium)));
        //    }

        //    var typedOperand = ((UntypedValue)crit.Operand).AsDateTimeValue();

        //    DateTimeOffset searchPeriodStart = typedOperand.ToDateTimeOffset();
        //    DateTimeOffset searchPeriodEnd = typedOperand.ToDateTimeOffset();

        //    return DateQuery(InternalField.LASTUPDATED, crit.Operator, crit.Modifier, (ValueExpression)crit.Operand);
        //}

        //internal static FilterDefinition<BsonDocument> _tagFixedQuery(Criterium crit)
        //{
        //    return TagQuery(crit, new Uri(XmlNs.FHIRTAG, UriKind.Absolute));
        //}

        //internal static FilterDefinition<BsonDocument> _profileFixedQuery(Criterium crit)
        //{
        //    return TagQuery(crit, new Uri(XmlNs.TAG_PROFILE, UriKind.Absolute));
        //}

        //internal static FilterDefinition<BsonDocument> _securityFixedQuery(Criterium crit)
        //{
        //    return TagQuery(crit, new Uri(XmlNs.TAG_SECURITY, UriKind.Absolute));
        //}

        //private static FilterDefinition<BsonDocument> TagQuery(Criterium crit, Uri tagscheme)
        //{
        //    if (crit.Operator == Operator.IN)
        //    {
        //        IEnumerable<ValueExpression> opMultiple = ((ChoiceValue)crit.Operand).Choices;
        //        var optionQueries = new List<FilterDefinition<BsonDocument>>();
        //        foreach (var choice in opMultiple)
        //        {
        //            Criterium option = new Criterium();
        //            option.Operator = Operator.EQ;
        //            option.Operand = choice;
        //            option.Modifier = crit.Modifier;
        //            option.ParamName = crit.ParamName;
        //            optionQueries.Add(TagQuery(option, tagscheme));
        //        }
        //        return Builders<BsonDocument>.Filter.Or(optionQueries);
        //    }

        //    //From here there's only 1 operand.
        //    FilterDefinition<BsonDocument> schemeQuery = Builders<BsonDocument>.Filter.Eq(InternalField.TAGSCHEME, tagscheme.AbsoluteUri);
        //    FilterDefinition<BsonDocument> argQuery;

        //    var operand = (ValueExpression)crit.Operand;
        //    switch (crit.Modifier)
        //    {
        //        case Modifier.PARTIAL:
        //            argQuery = StringQuery(InternalField.TAGTERM, Operator.EQ, Modifier.NONE, operand);
        //            break;
        //        case Modifier.TEXT:
        //            argQuery = StringQuery(InternalField.TAGLABEL, Operator.EQ, Modifier.NONE, operand);
        //            break;
        //        case Modifier.NONE:
        //        case null:
        //            argQuery = StringQuery(InternalField.TAGTERM, Operator.EQ, Modifier.EXACT, operand);
        //            break;
        //        default:
        //            throw new ArgumentException(String.Format("Invalid modifier {0} in parameter {1}", crit.Modifier, crit.ParamName));
        //    }

        //    return Builders<BsonDocument>.Filter.ElemMatch(InternalField.TAG, Builders<BsonDocument>.Filter.And(schemeQuery, argQuery));
        //}

        internal static FilterDefinition<BsonDocument> internal_justidFixedQuery(Criterium crit)
        {
            return StringQuery(InternalField.JUSTID, crit.Operator, "exact", (ValueExpression)crit.Operand);
        }

        //internal static FilterDefinition<BsonDocument> _idFixedQuery(Criterium crit)
        //{
        //    return StringQuery(InternalField.JUSTID, crit.Operator, "exact", (ValueExpression)crit.Operand);
        //}

        internal static FilterDefinition<BsonDocument> internal_idFixedQuery(Criterium crit)
        {
            return StringQuery(InternalField.ID, crit.Operator, "exact", (ValueExpression)crit.Operand);
        }

        private static FilterDefinition<BsonDocument> FalseQuery()
        {
            return Builders<BsonDocument>.Filter.Where(w => false);
        }

        private static FilterDefinition<BsonDocument> SafeIn(string parameterName, BsonArray values)
        {
            if (values.Any())
                return Builders<BsonDocument>.Filter.In(parameterName, values);
            return FalseQuery();
        }
    }
}
