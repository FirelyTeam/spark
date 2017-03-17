﻿/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using MongoDB.Driver;
using M = MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using System.Text.RegularExpressions;
using System.Reflection;
using Spark.Search.Support;
using Spark.Mongo.Search.Common;
using Spark.Engine.Extensions;
using Hl7.Fhir.Introspection;

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

        //internal static IMongoQuery ResourceFilter(this Query query)
        //{
        //    return ResourceFilter(query.ResourceType);
        //}

        internal static IMongoQuery ResourceFilter(string resourceType, int level)
        {
            var queries = new List<IMongoQuery>();
            if (level == 0)
                queries.Add(M.Query.EQ(InternalField.LEVEL, 0));
            queries.Add(M.Query.EQ(InternalField.RESOURCE, resourceType));

            return M.Query.And(queries);
        }

        internal static ModelInfo.SearchParamDefinition FindSearchParamDefinition(this Criterium param, string resourceType)
        {
            return param.SearchParameters?.FirstOrDefault(sp => sp.Resource == resourceType || sp.Resource == "Resource");

            //var sp = ModelInfo.SearchParameters;
            //return sp.Find(defn => defn.Name == param.ParamName && defn.Resource == resourceType);
        }

        internal static IMongoQuery ToFilter(this Criterium param, string resourceType)
        {
            //Maybe it's a generic parameter.
            MethodInfo methodForParameter = FixedQueries.Find(m => m.Name.Equals(param.ParamName + "FixedQuery"));
            if (methodForParameter != null)
            {
                return (IMongoQuery)methodForParameter.Invoke(null, new object[] { param });
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

        private static IMongoQuery SetParameter(this IMongoQuery query, string parameterName, IEnumerable<String> values)
        {
            return query.SetParameter(new BsonArray() { parameterName }.ToJson(), new BsonArray(values).ToJson());
        }

        private static IMongoQuery SetParameter(this IMongoQuery query, string parameterName, String value)
        {
            return new QueryDocument(BsonDocument.Parse(query.ToString().Replace(parameterName, value)));
        }

        private static IMongoQuery CreateFilter(ModelInfo.SearchParamDefinition parameter, Operator op, String modifier, Expression operand)
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
                        //return M.Query.Null;
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

        private static IMongoQuery StringQuery(String parameterName, Operator optor, String modifier, ValueExpression operand)
        {
            switch (optor)
            {
                case Operator.EQ:
                    var typedOperand = ((UntypedValue)operand).AsStringValue().ToString();
                    switch (modifier)
                    {
                        case Modifier.EXACT:
                            return M.Query.EQ(parameterName, typedOperand);
                        case Modifier.TEXT: //the same behaviour as :phonetic in previous versions.
                            return M.Query.Matches(parameterName + "soundex", "^" + typedOperand);
                        //case Modifier.BELOW:
                        //    return M.Query.Matches(parameterName, typedOperand + ".*")
                        case Modifier.NONE:
                        case null:
                            //partial from begin
                            return M.Query.Matches(parameterName, new BsonRegularExpression("^" + typedOperand, "i"));
                        default:
                            throw new ArgumentException(String.Format("Invalid modifier {0} on string parameter {1}", modifier, parameterName));
                    }
                case Operator.IN: //We'll only handle choice like :exact
                    IEnumerable<ValueExpression> opMultiple = ((ChoiceValue)operand).Choices;
                    return SafeIn(parameterName, new BsonArray(opMultiple.Cast<StringValue>().Select(sv => sv.Value)));
                case Operator.ISNULL:
                    return M.Query.Or(M.Query.NotExists(parameterName), M.Query.EQ(parameterName, BsonNull.Value)); //With only M.Query.NotExists, that would exclude resources that have this field with an explicit null in it.
                case Operator.NOTNULL:
                    return M.Query.NE(parameterName, BsonNull.Value); //We don't use M.Query.Exists, because that would include resources that have this field with an explicit null in it.
                default:
                    throw new ArgumentException(String.Format("Invalid operator {0} on string parameter {1}", optor.ToString(), parameterName));
            }
        }

        //No modifiers allowed on number parameters, hence not in the method signature.
        private static IMongoQuery NumberQuery(String parameterName, Operator optor, ValueExpression operand)
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
                    return M.Query.EQ(parameterName, typedOperand);
                case Operator.GT:
                    return M.Query.GT(parameterName, typedOperand);
                case Operator.GTE:
                    return M.Query.GTE(parameterName, typedOperand);
                case Operator.IN:
                    IEnumerable<ValueExpression> opMultiple = ((ChoiceValue)operand).Choices;
                    return SafeIn(parameterName, new BsonArray(opMultiple.Cast<NumberValue>().Select(nv => nv.Value)));
                case Operator.ISNULL:
                    return M.Query.EQ(parameterName, null);
                case Operator.LT:
                    return M.Query.LT(parameterName, typedOperand);
                case Operator.LTE:
                    return M.Query.LTE(parameterName, typedOperand);
                case Operator.NOTNULL:
                    return M.Query.NE(parameterName, null);
                default:
                    throw new ArgumentException(String.Format("Invalid operator {0} on number parameter {1}", optor.ToString(), parameterName));
            }
        }

        private static IMongoQuery ExpressionQuery(string name, Operator optor, BsonValue value)
        {
            switch (optor)
            {
                case Operator.EQ:
                    return M.Query.EQ(name, value);

                case Operator.GT:
                    return M.Query.GT(name, value);

                case Operator.GTE:
                    return M.Query.GTE(name, value);

                case Operator.ISNULL:
                    return M.Query.EQ(name, null);

                case Operator.LT:
                    return M.Query.LT(name, value);

                case Operator.LTE:
                    return M.Query.LTE(name, value);

                case Operator.NOTNULL:
                    return M.Query.NE(name, null);

                default:
                    throw new ArgumentException(String.Format("Invalid operator {0} on token parameter {1}", optor.ToString(), name));
            }
        }

        private static IMongoQuery QuantityQuery(String parameterName, Operator optor, String modifier, ValueExpression operand)
        {
            //$elemMatch only works on array values. But the MongoIndexMapper only creates an array if there are multiple values for a given parameter.
            //So we also construct a query for when there is only one set of values in the searchIndex, hence there is no array.
            var quantity = operand.ToModelQuantity();
            Fhir.Metrics.Quantity q = quantity.ToUnitsOfMeasureQuantity().Canonical();
            string decimals = q.SearchableString();
            BsonValue value = q.GetValueAsBson();

            List<IMongoQuery> arrayQueries = new List<IMongoQuery>();
            List<IMongoQuery> noArrayQueries = new List<IMongoQuery>() { M.Query.Not(M.Query.Type(parameterName, BsonType.Array)) };
            switch (optor)
            {
                case Operator.EQ:
                    arrayQueries.Add(M.Query.Matches("decimals", new BsonRegularExpression("^" + decimals)));
                    noArrayQueries.Add(M.Query.Matches(parameterName + ".decimals", new BsonRegularExpression("^" + decimals)));
                    break;

                default:
                    arrayQueries.Add(ExpressionQuery("value", optor, value));
                    noArrayQueries.Add(ExpressionQuery(parameterName + ".value", optor, value));
                    break;
            }

            if (quantity.System != null)
            {
                arrayQueries.Add(M.Query.EQ("system", quantity.System.ToString()));
                noArrayQueries.Add(M.Query.EQ(parameterName + ".system", quantity.System.ToString()));
            }
            arrayQueries.Add(M.Query.EQ("unit", q.Metric.ToString()));
            noArrayQueries.Add(M.Query.EQ(parameterName + ".unit", q.Metric.ToString()));

            var arrayQuery = M.Query.ElemMatch(parameterName, M.Query.And(arrayQueries));
            var noArrayQuery = M.Query.And(noArrayQueries);

            IMongoQuery query = M.Query.Or(arrayQuery, noArrayQuery);
            return query;
        }

        private static IMongoQuery TokenQuery(String parameterName, Operator optor, String modifier, ValueExpression operand)
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
                        return M.Query.Matches(textfield, new BsonRegularExpression(typedEqOperand.Value, "i"));
                    }
                    else //Search on code and system
                    {
                        //Set up two variants of queries, for dealing with single token values in the index, and multiple (in an array).
                        var arrayQueries = new List<IMongoQuery>();
                        var noArrayQueries = new List<IMongoQuery>(){
                            M.Query.Not(M.Query.Type(parameterName, BsonType.Array))};

                        if (modifier == Modifier.NOT) //NOT modifier only affects matching the code, not the system
                        {
                            noArrayQueries.Add(M.Query.Exists(parameterName));
                            noArrayQueries.Add(M.Query.NE(codefield, typedEqOperand.Value));
                            arrayQueries.Add(M.Query.Exists(parameterName));
                            arrayQueries.Add(M.Query.NE("code", typedEqOperand.Value));
                        }
                        else
                        {
                            noArrayQueries.Add(M.Query.EQ(codefield, typedEqOperand.Value));
                            arrayQueries.Add(M.Query.EQ("code", typedEqOperand.Value));
                        }

                        //Handle the system part, if present.
                        if (!typedEqOperand.AnyNamespace)
                        {
                            if (String.IsNullOrWhiteSpace(typedEqOperand.Namespace))
                            {
                                arrayQueries.Add(M.Query.NotExists("system"));
                                noArrayQueries.Add(M.Query.NotExists(systemfield));
                            }
                            else
                            {
                                arrayQueries.Add(M.Query.EQ("system", typedEqOperand.Namespace));
                                noArrayQueries.Add(M.Query.EQ(systemfield, typedEqOperand.Namespace));
                            }
                        }

                        //Combine code and system
                        var arrayEqQuery = M.Query.ElemMatch(parameterName, M.Query.And(arrayQueries));
                        var noArrayEqQuery = M.Query.And(noArrayQueries);
                        return M.Query.Or(arrayEqQuery, noArrayEqQuery);
                    }
                case Operator.IN:
                    IEnumerable<ValueExpression> opMultiple = ((ChoiceValue)operand).Choices;
                    return M.Query.Or(opMultiple.Select(choice => TokenQuery(parameterName, Operator.EQ, modifier, choice)));
                case Operator.ISNULL:
                    return M.Query.And(M.Query.EQ(parameterName, BsonNull.Value), M.Query.EQ(textfield, BsonNull.Value)); //We don't use M.Query.NotExists, because that would exclude resources that have this field with an explicit null in it.
                case Operator.NOTNULL:
                    return M.Query.Or(M.Query.NE(parameterName, BsonNull.Value), M.Query.EQ(textfield, BsonNull.Value)); //We don't use M.Query.Exists, because that would include resources that have this field with an explicit null in it.
                default:
                    throw new ArgumentException(String.Format("Invalid operator {0} on token parameter {1}", optor.ToString(), parameterName));
            }
        }

        private static IMongoQuery UriQuery(String parameterName, Operator optor, String modifier, ValueExpression operand)
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

        private static IMongoQuery DateQuery(String parameterName, Operator optor, String modifier, ValueExpression operand)
        {
            if (optor == Operator.IN)
            {
                IEnumerable<ValueExpression> opMultiple = ((ChoiceValue)operand).Choices;
                return M.Query.Or(opMultiple.Select(choice => DateQuery(parameterName, Operator.EQ, modifier, choice)));
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
                        M.Query.And(M.Query.GTE(end, valueLower), M.Query.LT(start, valueUpper));
                case Operator.GT:
                    return
                        M.Query.GTE(start, valueUpper);
                case Operator.GTE:
                    return
                        M.Query.GTE(start, valueLower);
                case Operator.LT:
                    return
                        M.Query.LT(end, valueLower);
                case Operator.LTE:
                    return
                        M.Query.LT(end, valueUpper);
                case Operator.ISNULL:
                    return M.Query.EQ(parameterName, null); //We don't use M.Query.NotExists, because that would exclude resources that have this field with an explicit null in it.
                case Operator.NOTNULL:
                    return M.Query.NE(parameterName, null); //We don't use M.Query.Exists, because that would include resources that have this field with an explicit null in it.
                default:
                    throw new ArgumentException(String.Format("Invalid operator {0} on date parameter {1}", optor.ToString(), parameterName));
            }
        }

        private static IMongoQuery CompositeQuery(ModelInfo.SearchParamDefinition parameterDef, Operator optor, String modifier, ValueExpression operand)
        {
            if (optor == Operator.IN)
            {
                var choices = ((ChoiceValue)operand);
                var queries = new List<IMongoQuery>();
                foreach (var choice in choices.Choices)
                {
                    queries.Add(CompositeQuery(parameterDef, Operator.EQ, modifier, choice));
                }
                return M.Query.Or(queries);
            }
            else if (optor == Operator.EQ)
            {
                var typedOperand = (CompositeValue)operand;
                var queries = new List<IMongoQuery>();
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
                return M.Query.And(queries);
            }
            throw new ArgumentException(String.Format("Invalid operator {0} on composite parameter {1}", optor.ToString(), parameterDef.Name));
        }

        //internal static IMongoQuery _lastUpdatedFixedQuery(Criterium crit)
        //{
        //    if (crit.Operator == Operator.IN)
        //    {
        //        IEnumerable<ValueExpression> opMultiple = ((ChoiceValue)crit.Operand).Choices;
        //        IEnumerable<Criterium> criteria = opMultiple.Select<ValueExpression, Criterium>(choice => new Criterium() { ParamName = crit.ParamName, Modifier = crit.Modifier, Operator = Operator.EQ, Operand = choice });
        //        return M.Query.Or(criteria.Select(criterium => _lastUpdatedFixedQuery(criterium)));
        //    }

        //    var typedOperand = ((UntypedValue)crit.Operand).AsDateTimeValue();

        //    DateTimeOffset searchPeriodStart = typedOperand.ToDateTimeOffset();
        //    DateTimeOffset searchPeriodEnd = typedOperand.ToDateTimeOffset();

        //    return DateQuery(InternalField.LASTUPDATED, crit.Operator, crit.Modifier, (ValueExpression)crit.Operand);
        //}

        //internal static IMongoQuery _tagFixedQuery(Criterium crit)
        //{
        //    return TagQuery(crit, new Uri(XmlNs.FHIRTAG, UriKind.Absolute));
        //}

        //internal static IMongoQuery _profileFixedQuery(Criterium crit)
        //{
        //    return TagQuery(crit, new Uri(XmlNs.TAG_PROFILE, UriKind.Absolute));
        //}

        //internal static IMongoQuery _securityFixedQuery(Criterium crit)
        //{
        //    return TagQuery(crit, new Uri(XmlNs.TAG_SECURITY, UriKind.Absolute));
        //}

        //private static IMongoQuery TagQuery(Criterium crit, Uri tagscheme)
        //{
        //    if (crit.Operator == Operator.IN)
        //    {
        //        IEnumerable<ValueExpression> opMultiple = ((ChoiceValue)crit.Operand).Choices;
        //        var optionQueries = new List<IMongoQuery>();
        //        foreach (var choice in opMultiple)
        //        {
        //            Criterium option = new Criterium();
        //            option.Operator = Operator.EQ;
        //            option.Operand = choice;
        //            option.Modifier = crit.Modifier;
        //            option.ParamName = crit.ParamName;
        //            optionQueries.Add(TagQuery(option, tagscheme));
        //        }
        //        return M.Query.Or(optionQueries);
        //    }

        //    //From here there's only 1 operand.
        //    IMongoQuery schemeQuery = M.Query.EQ(InternalField.TAGSCHEME, tagscheme.AbsoluteUri);
        //    IMongoQuery argQuery;

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

        //    return M.Query.ElemMatch(InternalField.TAG, M.Query.And(schemeQuery, argQuery));
        //}

        internal static IMongoQuery internal_justidFixedQuery(Criterium crit)
        {
            return StringQuery(InternalField.JUSTID, crit.Operator, "exact", (ValueExpression)crit.Operand);
        }

        //internal static IMongoQuery _idFixedQuery(Criterium crit)
        //{
        //    return StringQuery(InternalField.JUSTID, crit.Operator, "exact", (ValueExpression)crit.Operand);
        //}

        internal static IMongoQuery internal_idFixedQuery(Criterium crit)
        {
            return StringQuery(InternalField.ID, crit.Operator, "exact", (ValueExpression)crit.Operand);
        }

        private static IMongoQuery FalseQuery()
        {
            return M.Query.Where(@"false;");
        }

        private static IMongoQuery SafeIn(string parameterName, BsonArray values)
        {
            if (values.Any())
                return M.Query.In(parameterName, values);
            return FalseQuery();
        }
    }
}
