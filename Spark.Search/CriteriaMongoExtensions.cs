using Hl7.Fhir.Model;
using Hl7.Fhir.Search;
using MongoDB.Driver;
using M = MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Reflection;

[assembly: InternalsVisibleTo("Spark.Tests")]
namespace Spark.Search
{
    internal static class CriteriaMongoExtensions
    {
        internal static List<MethodInfo> FixedQueries = CacheQueryMethods();

        private static List<MethodInfo> CacheQueryMethods()
        {
            return typeof(CriteriaMongoExtensions).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Where(m => m.Name.EndsWith("FixedQuery")).ToList();
        }

        internal static IMongoQuery ResourceFilter(this Query query)
        {
            return ResourceFilter(query.ResourceType);
        }
        internal static IMongoQuery ResourceFilter(string resourceType)
        {
            var queries = new List<IMongoQuery>();
            queries.Add(M.Query.EQ(InternalField.LEVEL, 0));
            queries.Add(M.Query.EQ(InternalField.RESOURCE, resourceType));

            return M.Query.And(queries);
        }

        internal static IMongoQuery ToFilter(this Criterium crit, string resourceType)
        {
            //It could be a parameter as defined in the metadata
            var sp = ModelInfo.SearchParameters;
            var critSp = sp.Find(p => p.Name == crit.ParamName && p.Resource == resourceType);
            if (critSp != null)
            {
                return CreateFilter(critSp, crit.Type, crit.Modifier, crit.Operand);
            }

            //Maybe it's a generic parameter.
            MethodInfo methodForParameter = FixedQueries.Find(m => m.Name.Equals(crit.ParamName + "FixedQuery"));
            if (methodForParameter != null)
            {
                return (IMongoQuery)methodForParameter.Invoke(null, new object[] { crit });
            }
            throw new ArgumentException(String.Format("Resource {0} has no parameter with the name {1}.", resourceType, crit.ParamName));
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
                    default:
                        //return M.Query.Null;
                        throw new NotSupportedException("Only SearchParamType.Number or String is supported.");
                }
            }
        }

        internal static List<string> GetTargetedReferenceTypes(this Criterium chainCriterium)
        {
            if (chainCriterium.Type != Operator.CHAIN)
                throw new ArgumentException("Targeted reference types are only relevent for chained criteria.");

            var modifier = chainCriterium.Modifier;
            var nextInChain = (Criterium)chainCriterium.Operand;
            var parameter = nextInChain.ParamName;
            // The modifier contains the type of resource that the referenced resource must be. It is optional.
            // If not present, search all possible types of resources allowed at this reference.
            // If it is present, it should be of one of the possible types.

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

            // Afterwards, filter on the types that actually have the requested searchparameter.
            return searchResourceTypes.Where(rt => ModelInfo.SearchParameters.Exists(sp => rt.Equals(sp.Resource) && parameter.Equals(sp.Name))).ToList();
        }

        private static IMongoQuery StringQuery(String parameterName, Operator optor, String modifier, ValueExpression operand)
        {
            switch (optor)
            {
                case Operator.EQ:
                    var typedOperand = ((UntypedValue)operand).AsStringValue().ToString();
                    switch (modifier)
                    {
                        case "exact":
                            return M.Query.EQ(parameterName, typedOperand);
                        case "text": //the same behaviour as :phonetic in previous versions.
                            return M.Query.Matches(parameterName + "soundex", "^" + typedOperand);
                        default: //partial from begin
                            return M.Query.Matches(parameterName, new BsonRegularExpression("^" + typedOperand, "i"));
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
            var typedOperand = ((UntypedValue)operand).AsNumberValue().ToString();
            //May throw an InvalidCastException when operand is not a number...

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


        // This code might have a better place somewhere else: //mh
        private static Quantity ToQuantity(this ValueExpression expression)
        {
            QuantityValue q = QuantityValue.Parse(expression.ToString());
            Quantity quantity = new Quantity
            {
                Value = q.Number,
                System = (q.Namespace != null) ? new Uri(q.Namespace) : null,
                Units = q.Unit
            };
            return quantity;
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
            Quantity quantity = operand.ToQuantity().Standardize();
            string decimals = quantity.GetDecimalSearchableValue();

            List<IMongoQuery> queries = new List<IMongoQuery>();
            switch (optor)
            {
                case Operator.EQ:
                    queries.Add(M.Query.Matches("decimals", new BsonRegularExpression("^" + decimals, "i")));
                    break;

                default:
                    queries.Add(ExpressionQuery("value", optor, new BsonDouble((double)quantity.Value)));
                    break;
            }

            if (quantity.System != null)
                queries.Add(M.Query.EQ("system", quantity.System.ToString()));

            queries.Add(M.Query.EQ("unit", quantity.Units));

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
                        case "text":
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
                    return M.Query.EQ(parameterName, null); //We don't use M.Query.NotExists, because that would exclude resources that have this field with an explicit null in it.
                case Operator.NOTNULL:
                    return M.Query.NE(parameterName, null); //We don't use M.Query.Exists, because that would include resources that have this field with an explicit null in it.
                default:
                    throw new ArgumentException(String.Format("Invalid operator {0} on token parameter {1}", optor.ToString(), parameterName));
            }
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
            return TagQuery(crit, Tag.FHIRTAGSCHEME_GENERAL);
        }

        internal static IMongoQuery _profileFixedQuery(Criterium crit)
        {
            return TagQuery(crit, Tag.FHIRTAGSCHEME_PROFILE);
        }

        internal static IMongoQuery _securityFixedQuery(Criterium crit)
        {
            return TagQuery(crit, Tag.FHIRTAGSCHEME_SECURITY);
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
                    optionQueries.Add(_tagFixedQuery(option));
                }
                return M.Query.Or(optionQueries);
            }

            //From here there's only 1 operand.
            IMongoQuery schemeQuery = M.Query.EQ(InternalField.TAGSCHEME, tagscheme.AbsoluteUri);
            IMongoQuery argQuery;

            var typedOperand = ((UntypedValue)crit.Operand).AsStringValue();
            switch (crit.Modifier)
            {
                case "partial":
                    argQuery = M.Query.EQ(InternalField.TAGTERM, "^" + typedOperand.Value);
                    break;
                case "text":
                    argQuery = M.Query.EQ(InternalField.TAGLABEL, typedOperand.Value);
                    break;
                default:
                    argQuery = M.Query.EQ(InternalField.TAGTERM, typedOperand.Value);
                    break;
            }

            return M.Query.ElemMatch(InternalField.TAG, M.Query.And(schemeQuery, argQuery));
        }
    }
}
