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

namespace Spark.Search
{
    public static class CriteriaMongoExtensions
    {
        public static IMongoQuery ToQuery(this Query query)
        {
            //TODO: special parameters, includes.

            //Add the regular parameters.
            var queries = new List<IMongoQuery>();
            queries.Add(M.Query.EQ(InternalField.LEVEL, 0));
            queries.Add(M.Query.EQ(InternalField.RESOURCE, query.ResourceType));

            foreach (var qcrit in query.Criteria)
            {
                var crit = Criterium.Parse(qcrit);
                queries.Add(crit.ToQuery(query.ResourceType));
            }

            return M.Query.And(queries);
        }

        internal static IMongoQuery ToQuery(this Criterium crit, string resourceType)
        {
            var sp = ModelInfo.SearchParameters;
            var critSp = sp.Find(p => p.Name == crit.ParamName && p.Resource == resourceType);
            return CreateQuery(critSp, crit.Type, crit.Modifier, crit.Operand);
        }

        internal static IMongoQuery CreateQuery(ModelInfo.SearchParamDefinition parameter, Operator op, String modifier, Expression operand)
        {
            //Handle a list of operands recursively.
            if (op == Operator.IN)
            {
                IEnumerable<ValueExpression> opMultiple = ((ChoiceValue)operand).Choices;
                var subQueries = new List<IMongoQuery>();
                foreach (var opSingle in opMultiple)
                {
                    subQueries.Add(CreateQuery(parameter, Operator.EQ, modifier, opSingle));
                }
                return M.Query.Or(subQueries);
            }
            else // There's no IN operator and only one operand.
            { //LET OP: Chain heeft geen ValueExpression
                var valueOperand = (ValueExpression)operand;
                switch (parameter.Type)
                {
                    case Conformance.SearchParamType.Composite:
                    //TODO
                    case Conformance.SearchParamType.Date:
                    //TODO
                    case Conformance.SearchParamType.Number:
                        return NumberQuery(parameter.Name, op, valueOperand);
                    case Conformance.SearchParamType.Quantity:
                    //TODO
                    case Conformance.SearchParamType.Reference:
                    //TODO
                    case Conformance.SearchParamType.String:
                        return StringQuery(parameter.Name, op, modifier, valueOperand);
                    case Conformance.SearchParamType.Token:
                    //TODO
                    default:
                        //return M.Query.Null;
                        throw new NotSupportedException("Only SearchParamType.Number or String is supported.");
                }
            }
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
                case Operator.IN:
                //Invalid in this context, handled in CreateQuery().
                case Operator.ISNULL:
                    return M.Query.EQ(parameterName, null); //We don't use M.Query.NotExists, because that would exclude resources that have this field with an explicit null in it.
                case Operator.NOTNULL:
                    return M.Query.NE(parameterName, null); //We don't use M.Query.Exists, because that would include resources that have this field with an explicit null in it.
                default:
                    throw new ArgumentException(String.Format("Invalid operator {0} on string parameter {1}", optor.ToString(), parameterName));
            }
        }

        //No modifiers allowed on number parameters, hence not in the method signature.
        internal static IMongoQuery NumberQuery(String parameterName, Operator optor, ValueExpression operand)
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
                //Invalid in this context, handled in CreateQuery().
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
    }
}
