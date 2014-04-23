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

[assembly: InternalsVisibleTo("Spark.Tests")]
namespace Spark.Search
{
    internal static class CriteriaMongoExtensions
    {
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
            var sp = ModelInfo.SearchParameters;
            var critSp = sp.Find(p => p.Name == crit.ParamName && p.Resource == resourceType);
            if (critSp == null)
            {
                throw new ArgumentException(String.Format("Resource {0} has no parameter with the name {1}.", resourceType, crit.ParamName));
            }
            return CreateFilter(critSp, crit.Type, crit.Modifier, crit.Operand);
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
                    //TODO
                    //return CompositeQuery(parameter.Name, op, modifier, valueOperand);
                    case Conformance.SearchParamType.Date:
                    //TODO
                    //return DateQuery(parameter.Name, op, modifier, valueOperand);
                    case Conformance.SearchParamType.Number:
                        return NumberQuery(parameter.Name, op, valueOperand);
                    case Conformance.SearchParamType.Quantity:
                    //TODO
                    //return QuantityQuery(parameter.Name, op, modifier, valueOperand);
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
    }
}
