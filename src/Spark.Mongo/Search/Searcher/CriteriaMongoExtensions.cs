/* 
 * Copyright (c) 2014-2018, Firely (info@fire.ly) and contributors
 * Copyright (c) 2017-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
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
    internal static class CriteriaMongoExtensions
    {
        private static readonly List<MethodInfo> _fixedQueries = CacheQueryMethods();

        private static List<MethodInfo> CacheQueryMethods()
        {
            return typeof(CriteriaMongoExtensions).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Where(m => m.Name.EndsWith("FixedQuery")).ToList();
        }

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
        }

        internal static FilterDefinition<BsonDocument> ToFilter(this Criterium param, string resourceType)
        {
            //Maybe it's a generic parameter.
            MethodInfo methodForParameter = _fixedQueries.Find(m => m.Name.Equals(param.ParamName + "FixedQuery"));
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

            throw new ArgumentException(string.Format("Resource {0} has no parameter with the name {1}.", resourceType, param.ParamName));
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
                {
                    parameterName = "fhir_id"; //See MongoIndexMapper for counterpart.

                    // This search finds the patient resource with the given id (there can only be one resource for a given id).
                    // Functionally, this is equivalent to a simple read operation
                    modifier = Modifier.EXACT;
                }

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
                        return QuantityQuery(parameterName, op, valueOperand);
                    case SearchParamType.Reference:
                        //Chain is handled in MongoSearcher, so here we have the result of a closed criterium: IN [ list of id's ]
                        if (parameter.Target?.Any() == true && modifier != Modifier.IDENTIFIER && valueOperand != null && !valueOperand.ToUnescapedString().Contains("/"))
                        {
                            // For searching by reference without type specified.
                            // If reference target type is known, create the exact query like ^(Person|Group)/(123|456)$
                            return Builders<BsonDocument>.Filter.Regex(parameterName,
                                new BsonRegularExpression(new Regex(
                                    $"^({string.Join("|", parameter.Target)})/({valueOperand.ToUnescapedString().Replace(",", "|")})$")));
                        }
                        else if (modifier == Modifier.IDENTIFIER)
                        {
                            return TokenQuery(parameterName, op, Modifier.EXACT, valueOperand);
                        }
                        else
                        {
                            return StringQuery(parameterName, op, Modifier.EXACT, valueOperand);
                        }
                    case SearchParamType.String:
                        return StringQuery(parameterName, op, modifier, valueOperand);
                    case SearchParamType.Token:
                        return TokenQuery(parameterName, op, modifier, valueOperand);
                    case SearchParamType.Uri:
                        return UriQuery(parameterName, op, modifier, valueOperand);
                    default:
                        //return Builders<BsonDocument>.Filter.Null;
                        throw new NotSupportedException(string.Format("SearchParamType {0} on parameter {1} not supported.", parameter.Type, parameter.Name));
                }
            }
        }

        private static List<string> GetTargetedReferenceTypes(ModelInfo.SearchParamDefinition parameter, String modifier)
        {
            var allowedResourceTypes = parameter.Target.Select(t => EnumUtility.GetLiteral(t)).ToList();// ModelInfo.SupportedResources; //TODO: restrict to parameter.ReferencedResources. This means not making this static, because you want to use IFhirModel.
            List<string> searchResourceTypes = new List<string>();
            if (string.IsNullOrEmpty(modifier))
                searchResourceTypes.AddRange(allowedResourceTypes);
            else if (allowedResourceTypes.Contains(modifier))
            {
                searchResourceTypes.Add(modifier);
            }
            else
            {
                throw new NotSupportedException(string.Format("Referenced type cannot be of type %s.", modifier));
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
                    var typedOperand = operand.ToUnescapedString();
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
                            throw new ArgumentException(string.Format("Invalid modifier {0} on string parameter {1}", modifier, parameterName));
                    }
                case Operator.IN: //We'll only handle choice like :exact
                    IEnumerable<ValueExpression> opMultiple = ((ChoiceValue)operand).Choices;
                    return SafeIn(parameterName, new BsonArray(opMultiple.Select(sv => sv.ToUnescapedString())));
                case Operator.ISNULL:
                    return Builders<BsonDocument>.Filter.Or(Builders<BsonDocument>.Filter.Exists(parameterName, false), Builders<BsonDocument>.Filter.Eq(parameterName, BsonNull.Value)); //With only Builders<BsonDocument>.Filter.NotExists, that would exclude resources that have this field with an explicit null in it.
                case Operator.NOTNULL:
                    return Builders<BsonDocument>.Filter.Ne(parameterName, BsonNull.Value); //We don't use Builders<BsonDocument>.Filter.Exists, because that would include resources that have this field with an explicit null in it.
                default:
                    throw new ArgumentException(string.Format("Invalid operator {0} on string parameter {1}", optor.ToString(), parameterName));
            }
        }

        //No modifiers allowed on number parameters, hence not in the method signature.
        private static FilterDefinition<BsonDocument> NumberQuery(String parameterName, Operator optor, ValueExpression operand)
        {
            string typedOperand = null;
            if (operand != null)
            {
                try
                {
                    typedOperand = ((UntypedValue)operand).AsNumberValue().ToString();
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException(string.Format("Invalid number value {0} on number parameter {1}", operand, parameterName));
                }
                catch (FormatException)
                {
                    throw new ArgumentException(string.Format("Invalid number value {0} on number parameter {1}", operand, parameterName));
                }
            }

            switch (optor)
            {
                //case Operator.APPROX:
                //TODO
                //case Operator.CHAIN:
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
                case Operator.LT:
                    return Builders<BsonDocument>.Filter.Lt(parameterName, typedOperand);
                case Operator.LTE:
                    return Builders<BsonDocument>.Filter.Lte(parameterName, typedOperand);
                case Operator.NOT_EQUAL:
                    return Builders<BsonDocument>.Filter.Ne(parameterName, typedOperand);
                case Operator.ISNULL:
                    return Builders<BsonDocument>.Filter.Or(Builders<BsonDocument>.Filter.Exists(parameterName, false), Builders<BsonDocument>.Filter.Eq(parameterName, BsonNull.Value)); //With only Builders<BsonDocument>.Filter.NotExists, that would exclude resources that have this field with an explicit null in it.
                case Operator.NOTNULL:
                    return Builders<BsonDocument>.Filter.Ne(parameterName, BsonNull.Value); //We don't use Builders<BsonDocument>.Filter.Exists, because that would include resources that have this field with an explicit null in it.
                default:
                    throw new ArgumentException(string.Format("Invalid operator {0} on number parameter {1}", optor.ToString(), parameterName));
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
                    throw new ArgumentException(string.Format("Invalid operator {0} on token parameter {1}", optor.ToString(), name));
            }
        }

        private static FilterDefinition<BsonDocument> QuantityQuery(string parameterName, Operator optor, ValueExpression operand)
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
                        var noArrayQueries = new List<FilterDefinition<BsonDocument>>{
                            Builders<BsonDocument>.Filter.Not(Builders<BsonDocument>.Filter.Type(parameterName, BsonType.Array))};
                        var plainStringQueries = new List<FilterDefinition<BsonDocument>>{
                            Builders<BsonDocument>.Filter.Type(parameterName, BsonType.String)};

                        if (!string.IsNullOrEmpty(typedEqOperand.Value))
                        {
                            noArrayQueries.Add(Builders<BsonDocument>.Filter.Eq(codefield, typedEqOperand.Value));
                            arrayQueries.Add(Builders<BsonDocument>.Filter.Eq("code", typedEqOperand.Value));
                            plainStringQueries.Add(Builders<BsonDocument>.Filter.Eq(parameterName, typedEqOperand.Value));
                        }

                        //Handle the system part, if present.
                        if (!typedEqOperand.AnyNamespace)
                        {
                            if (string.IsNullOrWhiteSpace(typedEqOperand.Namespace))
                            {
                                arrayQueries.Add(Builders<BsonDocument>.Filter.Exists("system", false));
                                noArrayQueries.Add(Builders<BsonDocument>.Filter.Exists(systemfield, false));
                                plainStringQueries.Add(Builders<BsonDocument>.Filter.Exists("system", false));
                            }
                            else
                            {
                                arrayQueries.Add(Builders<BsonDocument>.Filter.Eq("system", typedEqOperand.Namespace));
                                noArrayQueries.Add(Builders<BsonDocument>.Filter.Eq(systemfield, typedEqOperand.Namespace));
                                plainStringQueries.Add(Builders<BsonDocument>.Filter.Eq("system", typedEqOperand.Namespace));
                            }
                        }

                        //Combine code and system
                        var arrayEqQuery = Builders<BsonDocument>.Filter.ElemMatch(parameterName, Builders<BsonDocument>.Filter.And(arrayQueries));
                        var noArrayEqQuery = Builders<BsonDocument>.Filter.And(noArrayQueries);
                        var plainStringQuery = Builders<BsonDocument>.Filter.And(plainStringQueries);
                        return modifier == Modifier.NOT ?
                             Builders<BsonDocument>.Filter.And(Builders<BsonDocument>.Filter.Not(arrayEqQuery),
                             Builders<BsonDocument>.Filter.Not(noArrayEqQuery), Builders<BsonDocument>.Filter.Not(plainStringQuery))
                            : Builders<BsonDocument>.Filter.Or(arrayEqQuery, noArrayEqQuery, plainStringQuery);
                    }
                case Operator.IN:
                    IEnumerable<ValueExpression> opMultiple = ((ChoiceValue)operand).Choices;
                    var queries = opMultiple.Select(choice => TokenQuery(parameterName, Operator.EQ, modifier, choice));
                    return modifier == Modifier.NOT ? Builders<BsonDocument>.Filter.And(queries) : Builders<BsonDocument>.Filter.Or(queries);
                case Operator.ISNULL:
                    return Builders<BsonDocument>.Filter.And(Builders<BsonDocument>.Filter.Eq(parameterName, BsonNull.Value), Builders<BsonDocument>.Filter.Eq(textfield, BsonNull.Value)); //We don't use Builders<BsonDocument>.Filter.NotExists, because that would exclude resources that have this field with an explicit null in it.
                case Operator.NOTNULL:
                    return Builders<BsonDocument>.Filter.Or(Builders<BsonDocument>.Filter.Ne(parameterName, BsonNull.Value), Builders<BsonDocument>.Filter.Eq(textfield, BsonNull.Value)); //We don't use Builders<BsonDocument>.Filter.Exists, because that would include resources that have this field with an explicit null in it.
                default:
                    throw new ArgumentException($"Invalid operator {optor} on token parameter {parameterName}");
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
                    throw new NotImplementedException(string.Format("Modifier {0} on Uri parameter {1} not supported yet.", modifier, parameterName));
                case Modifier.NONE:
                case null:
                    localModifier = Modifier.EXACT;
                    break;
                case Modifier.MISSING:
                    localModifier = Modifier.MISSING;
                    break;
                default:
                    throw new ArgumentException(string.Format("Invalid modifier {0} on Uri parameter {1}", modifier, parameterName));
            }
            return StringQuery(parameterName, optor, localModifier, operand == null ? null : new UntypedValue(UriUtil.NormalizeUri(operand.ToUnescapedString())));
        }

        private static FilterDefinition<BsonDocument> DateQuery(String parameterName, Operator optor, String modifier, ValueExpression operand)
        {
            if (optor == Operator.IN)
            {
                IEnumerable<ValueExpression> opMultiple = ((ChoiceValue)operand).Choices;
                return Builders<BsonDocument>.Filter.Or(opMultiple.Select(choice => DateQuery(parameterName, Operator.EQ, modifier, choice)));
            }

            var start = parameterName + ".start";
            var end = parameterName + ".end";

            BsonDateTime dateValueLower = null;
            BsonDateTime dateValueUpper = null;
            if (operand != null)
            {
                var dateValue = ((UntypedValue)operand).AsDateTimeValue();
                dateValueLower = BsonDateTime.Create(dateValue.LowerBound());
                dateValueUpper = BsonDateTime.Create(dateValue.UpperBound());
            }

            switch (optor)
            {
                case Operator.APPROX:
                case Operator.EQ:
                    return Builders<BsonDocument>.Filter.And(
                            Builders<BsonDocument>.Filter.Gte(end, dateValueLower),
                            Builders<BsonDocument>.Filter.Lt(start, dateValueUpper)
                        );
                case Operator.NOT_EQUAL:
                    return Builders<BsonDocument>.Filter.Or(
                            Builders<BsonDocument>.Filter.Lte(end, dateValueLower),
                            Builders<BsonDocument>.Filter.Gte(start, dateValueUpper)
                        );
                case Operator.GT:
                    return Builders<BsonDocument>.Filter.Gte(start, dateValueUpper);
                case Operator.GTE:
                    return Builders<BsonDocument>.Filter.Gte(start, dateValueLower);
                case Operator.LT:
                    return Builders<BsonDocument>.Filter.Lt(end, dateValueLower);
                case Operator.LTE:
                    return Builders<BsonDocument>.Filter.Lte(end, dateValueUpper);
                case Operator.STARTS_AFTER:
                    return Builders<BsonDocument>.Filter.Gte(start, dateValueUpper);
                case Operator.ENDS_BEFORE:
                    return Builders<BsonDocument>.Filter.Lte(end, dateValueLower);
                case Operator.ISNULL:
                    return Builders<BsonDocument>.Filter.Or(Builders<BsonDocument>.Filter.Exists(parameterName, false), Builders<BsonDocument>.Filter.Eq(parameterName, BsonNull.Value)); //With only Builders<BsonDocument>.Filter.NotExists, that would exclude resources that have this field with an explicit null in it.
                case Operator.NOTNULL:
                    return Builders<BsonDocument>.Filter.Ne(parameterName, BsonNull.Value); //We don't use Builders<BsonDocument>.Filter.Exists, because that would include resources that have this field with an explicit null in it.
                default:
                    throw new ArgumentException($"Invalid operator {optor} on date parameter {parameterName}");
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
                var subParams = parameterDef.Component;

                if (components.Count() != subParams.Count())
                {
                    throw new ArgumentException(string.Format("Parameter {0} requires exactly {1} composite values, not the currently provided {2} values.", parameterDef.Name, subParams.Count(), components.Count()));
                }

                for (int i = 0; i < subParams.Count(); i++)
                {
                    var subCrit = new Criterium
                    {
                        Operator = Operator.EQ,
                        ParamName = subParams[i].Definition,
                        Operand = components[i],
                        Modifier = modifier
                    };
                    queries.Add(subCrit.ToFilter(parameterDef.Resource));
                }
                return Builders<BsonDocument>.Filter.And(queries);
            }
            throw new ArgumentException(string.Format("Invalid operator {0} on composite parameter {1}", optor.ToString(), parameterDef.Name));
        }

        internal static FilterDefinition<BsonDocument> internal_justidFixedQuery(Criterium crit)
        {
            return StringQuery(InternalField.JUSTID, crit.Operator, "exact", (ValueExpression)crit.Operand);
        }

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
