/* 
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

namespace Spark.Search.Mongo
{

    // todo: DSTU2 - NonExistent classes: Operator, Expression, ValueExpression

    internal static class CriteriaMongoExtensions
    {
        internal static List<MethodInfo> FixedQueries = CacheQueryMethods();

        private static List<MethodInfo> CacheQueryMethods()
        {
            return typeof(CriteriaMongoExtensions).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Where(m => m.Name.EndsWith("FixedQuery")).ToList();
        }

        //internal static IMongoQuery ResourceFilter(this Query query)
        //{
        //    return ResourceFilter(query.ResourceType);
        //}

        internal static IMongoQuery ResourceFilter(string resourceType)
        {
            var queries = new List<IMongoQuery>();
            queries.Add(M.Query.EQ(InternalField.LEVEL, 0));
            queries.Add(M.Query.EQ(InternalField.RESOURCE, resourceType));

            return M.Query.And(queries);
        }

        internal static ModelInfo.SearchParamDefinition FindSearchParamDefinition(this Criterium param, string resourceType)
        {
            var sp = ModelInfo.SearchParameters;
            return sp.Find(defn => defn.Name == param.ParamName && defn.Resource == resourceType);
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
                return CreateFilter(critSp, param.Type, param.Modifier, param.Operand);
                //return null;
            }

            throw new ArgumentException(String.Format("Resource {0} has no parameter with the name {1}.", resourceType, param.ParamName));
        }

        internal static IMongoQuery SetParameter(this IMongoQuery query, string parameterName, IEnumerable<String> values)
        {
            return query.SetParameter(new BsonArray() { parameterName }.ToJson(), new BsonArray(values).ToJson());
        }

        internal static IMongoQuery SetParameter(this IMongoQuery query, string parameterName, String value)
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
                var valueOperand = (ValueExpression)operand;
                switch (parameter.Type)
                {
                    case Conformance.SearchParamType.Composite:
                        return CompositeQuery(parameter, op, modifier, valueOperand);
                    case Conformance.SearchParamType.Date:
                        return DateQuery(parameter.Name, op, modifier, valueOperand);
                    case Conformance.SearchParamType.Number:
                        return NumberQuery(parameter.Name, op, valueOperand);
                    case Conformance.SearchParamType.Quantity:
                        return QuantityQuery(parameter.Name, op, modifier, valueOperand);
                    case Conformance.SearchParamType.Reference:
                        //Chain is handled in MongoSearcher, so here we have the result of a closed criterium: IN [ list of id's ]
                        return StringQuery(parameter.Name, op, modifier, valueOperand);
                    case Conformance.SearchParamType.String:
                        return StringQuery(parameter.Name, op, modifier, valueOperand);
                    case Conformance.SearchParamType.Token:
                        return TokenQuery(parameter.Name, op, modifier, valueOperand);
                    case Conformance.SearchParamType.Uri:
                        return UriQuery(parameter.Name, op, modifier, valueOperand);
                    default:
                        //return M.Query.Null;
                        throw new NotSupportedException(String.Format("SearchParamType {0} on parameter {1} not supported.", parameter.Type, parameter.Name));
                }
            }
        }

        private static List<string> GetTargetedReferenceTypes(ModelInfo.SearchParamDefinition parameter, String modifier)
        {
            var allowedResourceTypes = ModelInfo.SupportedResources; //TODO: restrict to parameter.ReferencedResources
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
            
            if (chainCriterium.Type != Operator.CHAIN)
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
            return searchResourceTypes.Where(rt => InternalField.All.Contains(nextParameter) || ModelInfo.SearchParameters.Exists(sp => rt.Equals(sp.Resource) && nextParameter.Equals(sp.Name))).ToList();
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
                    return M.Query.In(parameterName, new BsonArray(opMultiple.Cast<UntypedValue>().Select(uv => uv.AsStringValue().ToString())));
                case Operator.ISNULL:
                    return M.Query.EQ(parameterName, null); //We don't use M.Query.NotExists, because that would exclude resources that have this field with an explicit null in it.
                case Operator.NOTNULL:
                    return M.Query.NE(parameterName, null); //We don't use M.Query.Exists, because that would include resources that have this field with an explicit null in it.
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
            catch(FormatException)
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
                    return M.Query.In(parameterName, new BsonArray(opMultiple.Cast<UntypedValue>().Select(uv => uv.AsNumberValue().ToString())));
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

        public static IMongoQuery ExpressionQuery(string name, Operator optor, BsonValue value)
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
            var quantity = operand.ToModelQuantity();
            Fhir.Metrics.Quantity q = quantity.ToUnitsOfMeasureQuantity().Canonical();
            string decimals = UnitsOfMeasureHelper.SearchableString(q);
            BsonValue value = q.GetValueAsBson();
            
            List<IMongoQuery> queries = new List<IMongoQuery>();
            switch (optor)
            {
                case Operator.EQ:
                    queries.Add(M.Query.Matches("decimals", new BsonRegularExpression("^" + decimals)));
                    break;

                default:
                    queries.Add(ExpressionQuery("value", optor, value));
                    break;
            }

            if (quantity.System != null)
                queries.Add(M.Query.EQ("system", quantity.System.ToString()));

            queries.Add(M.Query.EQ("unit", q.Metric.ToString()));

            IMongoQuery query = M.Query.ElemMatch(parameterName, M.Query.And(queries));
            return query;
        }

        private static IMongoQuery TokenQuery(String parameterName, Operator optor, String modifier, ValueExpression operand)
        {
            string systemfield = parameterName + ".system";
            string codefield = parameterName + ".code";
            string displayfield = parameterName + ".display";
            string textfield = parameterName + "_text";

            switch (optor)
            {
                case Operator.EQ:
                    var typedOperand = ((UntypedValue)operand).AsTokenValue();
                    switch (modifier)
                    {
                        case Modifier.TEXT:
                            return M.Query.Or(
                                M.Query.Matches(textfield, new BsonRegularExpression(typedOperand.Value, "i")),
                                M.Query.Matches(displayfield, new BsonRegularExpression(typedOperand.Value, "i")));

                        default:
                            if (typedOperand.AnyNamespace)
                                return M.Query.EQ(codefield, typedOperand.Value);
                            else if (String.IsNullOrWhiteSpace(typedOperand.Namespace))
                                return M.Query.ElemMatch(parameterName,
                                        M.Query.And(
                                            M.Query.NotExists("system"),
                                            M.Query.EQ("code", typedOperand.Value)
                                        ));
                            else
                                return M.Query.ElemMatch(parameterName,
                                        M.Query.And(
                                            M.Query.EQ("system", typedOperand.Namespace),
                                            M.Query.EQ("code", typedOperand.Value)
                                        ));
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

            var typedOperand = ((UntypedValue)operand).AsDateValue();
            var value = GroomDate(typedOperand.Value);

            switch (optor)
            {
                case Operator.EQ:
                    return
                        M.Query.Or(
                            M.Query.Matches(parameterName, "^" + value),
                            M.Query.And(
                                M.Query.Or(M.Query.Exists(start), M.Query.Exists(end)),
                                M.Query.Or(M.Query.LTE(start, value), M.Query.NotExists(start)),
                                M.Query.Or(M.Query.GTE(end, value), M.Query.NotExists(end))
                            )
                        );
                case Operator.GT:
                    return
                        M.Query.Or(
                            M.Query.GT(parameterName, value),
                            M.Query.GT(start, value)
                        );
                case Operator.GTE:
                    return
                        M.Query.Or(
                            M.Query.GTE(parameterName, value),
                            M.Query.GTE(start, value)
                        );
                case Operator.LT:
                    return
                        M.Query.Or(
                            M.Query.LT(parameterName, value),
                            M.Query.LT(end, value)
                        );
                case Operator.LTE:
                    return
                        M.Query.Or(
                            M.Query.LTE(parameterName, value),
                            M.Query.LTE(end, value)
                        );
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
                    subCrit.Type = Operator.EQ;
                    subCrit.ParamName = subParams[i];
                    subCrit.Operand = components[i];
                    subCrit.Modifier = modifier;
                    queries.Add(subCrit.ToFilter(parameterDef.Resource));
                }
                return M.Query.And(queries);
            }
            throw new ArgumentException(String.Format("Invalid operator {0} on composite parameter {1}", optor.ToString(), parameterDef.Name));
        }

        internal static IMongoQuery _tagFixedQuery(Criterium crit)
        {
            return TagQuery(crit, new Uri(XmlNs.FHIRTAG, UriKind.Absolute));
        }

        internal static IMongoQuery _profileFixedQuery(Criterium crit)
        {
            return TagQuery(crit, new Uri(XmlNs.TAG_PROFILE, UriKind.Absolute));
        }

        internal static IMongoQuery _securityFixedQuery(Criterium crit)
        {
            return TagQuery(crit, new Uri(XmlNs.TAG_SECURITY, UriKind.Absolute));
        }

        private static IMongoQuery TagQuery(Criterium crit, Uri tagscheme)
        {
            if (crit.Type == Operator.IN)
            {
                IEnumerable<ValueExpression> opMultiple = ((ChoiceValue)crit.Operand).Choices;
                var optionQueries = new List<IMongoQuery>();
                foreach (var choice in opMultiple)
                {
                    Criterium option = new Criterium();
                    option.Type = Operator.EQ;
                    option.Operand = choice;
                    option.Modifier = crit.Modifier;
                    option.ParamName = crit.ParamName;
                    optionQueries.Add(TagQuery(option, tagscheme));
                }
                return M.Query.Or(optionQueries);
            }

            //From here there's only 1 operand.
            IMongoQuery schemeQuery = M.Query.EQ(InternalField.TAGSCHEME, tagscheme.AbsoluteUri);
            IMongoQuery argQuery;

            var operand = (ValueExpression)crit.Operand;
            switch (crit.Modifier)
            {
                case Modifier.PARTIAL:
                    argQuery = StringQuery(InternalField.TAGTERM, Operator.EQ, Modifier.NONE, operand);
                    break;
                case Modifier.TEXT:
                    argQuery = StringQuery(InternalField.TAGLABEL, Operator.EQ, Modifier.NONE, operand);
                    break;
                case Modifier.NONE:
                case null:
                    argQuery = StringQuery(InternalField.TAGTERM, Operator.EQ, Modifier.EXACT, operand);
                    break;
                default:
                    throw new ArgumentException(String.Format("Invalid modifier {0} in parameter {1}", crit.Modifier, crit.ParamName));
            }

            return M.Query.ElemMatch(InternalField.TAG, M.Query.And(schemeQuery, argQuery));
        }

        internal static IMongoQuery _idFixedQuery(Criterium crit)
        {
            return StringQuery(InternalField.JUSTID, crit.Type, "exact", (ValueExpression)crit.Operand);
        }

        internal static IMongoQuery internal_idFixedQuery(Criterium crit)
        {
            return StringQuery(InternalField.ID, crit.Type, "exact", (ValueExpression)crit.Operand);
        }
    }
}
