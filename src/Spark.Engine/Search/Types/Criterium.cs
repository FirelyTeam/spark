/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2019-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Engine.Extensions;
using Spark.Engine.Search.Support;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Engine.Search.Types;

/// <summary>
///     Types of comparison operator applicable to searching on integer values
/// </summary>
public enum Operator
{
    LT, // less than
    LTE, // less than or equals
    EQ, // equals (default)
    APPROX, // approximately equals
    GTE, // greater than or equals
    GT, // greater than

    ISNULL, // has no value
    NOTNULL, // has value
    IN, // equals one of a set of values
    CHAIN, // chain to subexpression
    NOT_EQUAL, // not equal
    STARTS_AFTER,
    ENDS_BEFORE
}

public class Criterium : Expression, ICloneable
{
    private const string MISSING_MODIF = "missing";
    private const string MISSING_TRUE = "true";
    private const string MISSING_FALSE = "false";

    //CK: Order of these mappings is important for string matching. From more specific to less specific.
    private static readonly List<Tuple<string, Operator>> OPERATOR_MAPPING =
    [
        new("ne", Operator.NOT_EQUAL),
        new("ge", Operator.GTE),
        new("le", Operator.LTE),
        new("gt", Operator.GT),
        new("lt", Operator.LT),
        new("sa", Operator.STARTS_AFTER),
        new("eb", Operator.ENDS_BEFORE),
        new("ap", Operator.APPROX),
        new("eq", Operator.EQ),
        new("IN", Operator.IN),
        new("", Operator.EQ)
    ];

    private List<ModelInfo.SearchParamDefinition> _searchParameters;

    public string ParamName { get; set; }

    public Operator Operator { get; set; } = Operator.EQ;

    public string Modifier { get; set; }

    public Expression Operand { get; set; }

    //CK: TODO: This should be SearchParameter, but that does not support Composite parameters very well.
    public List<ModelInfo.SearchParamDefinition> SearchParameters => _searchParameters ??= [];

    object ICloneable.Clone() => Clone();

    public static Criterium Parse(string resourceType, string key, string value)
    {
        if (string.IsNullOrEmpty(key)) throw Error.ArgumentNull("key");
        if (string.IsNullOrEmpty(value)) throw Error.ArgumentNull("value");

        // Split chained parts (if any) into name + modifier tuples
        Tuple<string, string>[] chainPath = key
            .Split([SearchParams.SEARCH_CHAINSEPARATOR], StringSplitOptions.RemoveEmptyEntries)
            .Select(pathToKeyModifTuple)
            .ToArray();

        return chainPath.Any()
            ? fromPathTuples(chainPath, value, resourceType)
            : throw Error.Argument("key", "Supplied an empty search parameter name or chain");
    }

    private static Criterium Parse(string key, string value)
    {
        if (string.IsNullOrEmpty(key)) throw Error.ArgumentNull("key");
        if (string.IsNullOrEmpty(value)) throw Error.ArgumentNull("value");

        // Split chained parts (if any) into name + modifier tuples
        Tuple<string, string>[] chainPath = key
            .Split([SearchParams.SEARCH_CHAINSEPARATOR], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => pathToKeyModifTuple(s))
            .ToArray();

        if (!chainPath.Any()) throw Error.Argument("key", "Supplied an empty search parameter name or chain");

        return fromPathTuples(chainPath, value);
    }

    [Obsolete("Use Parse(string resourceType, string key, string value)")]
    public static Criterium Parse(string text)
    {
        if (string.IsNullOrEmpty(text)) throw Error.ArgumentNull("text");

        Tuple<string, string> keyVal = text.SplitLeft('=');

        if (keyVal.Item2 == null) throw Error.Argument("text", "Value must contain an '=' to separate key and value");

        return Parse(keyVal.Item1, keyVal.Item2);
    }

    public override string ToString()
    {
        string result = ParamName;

        // Turn ISNULL and NOTNULL operators into the :missing modifier
        if (Operator == Operator.ISNULL || Operator == Operator.NOTNULL)
            result += SearchParams.SEARCH_MODIFIERSEPARATOR + MISSING_MODIF;
        else if (!string.IsNullOrEmpty(Modifier)) result += SearchParams.SEARCH_MODIFIERSEPARATOR + Modifier;

        if (Operator == Operator.CHAIN)
        {
            if (Operand is Criterium)
                return result + SearchParams.SEARCH_CHAINSEPARATOR + Operand;
            return result + SearchParams.SEARCH_CHAINSEPARATOR +
                   " ** INVALID CHAIN OPERATION ** Chain operation must have a Criterium as operand";
        }

        return result + "=" + BuildValue();
    }

    private static Tuple<string, string> pathToKeyModifTuple(string pathPart)
    {
        string[] pair = pathPart.Split(SearchParams.SEARCH_MODIFIERSEPARATOR);

        string name = pair[0];
        string modifier = pair.Length == 2 ? pair[1] : null;

        return Tuple.Create(name, modifier);
    }

    private static Criterium fromPathTuples(Span<Tuple<string, string>> path, string value,
        string resourceType = null)
    {
        Tuple<string, string> first = path[0];
        string name = first.Item1;
        string modifier = first.Item2;
        Operator type = Operator.EQ;
        Expression operand;

        // If this is a chained search, unfold the chain first
        if (path.Length > 1)
        {
            type = Operator.CHAIN;
            operand = fromPathTuples(path.Slice(1), value, resourceType);
        }

        // :missing modifier is actually not a real modifier and is turned into
        // either a ISNULL or NOTNULL operator
        else if (modifier == MISSING_MODIF)
        {
            modifier = null;

            type = value switch
            {
                MISSING_TRUE => Operator.ISNULL,
                MISSING_FALSE => Operator.NOTNULL,
                _ => throw Error.Argument("value",
                    "For the :missing modifier, only values 'true' and 'false' are allowed")
            };

            operand = null;
        }
        // else see if the value starts with a comparator
        else
        {
            // If this an ordered parameter type, then we accept a comparator prefix: https://www.hl7.org/fhir/stu3/search.html#prefix
            if (ModelInfo.SearchParameters.CanHaveOperatorPrefix(resourceType, name))
            {
                Tuple<Operator, string> compVal = findComparator(value);
                type = compVal.Item1;
                value = compVal.Item2;
            }

            if (value == null) throw new FormatException("Value is empty");
            // Parse the value. If there's > 1, we are using the IN operator, unless
            // the input already specifies another comparison, which would be illegal
            ChoiceValue values = ChoiceValue.Parse(value);

            if (values.Choices.Length > 1)
            {
                if (type != Operator.EQ)
                {
                    throw new InvalidOperationException(
                        "Multiple values cannot be used in combination with a comparison operator");
                }

                type = Operator.IN;
                operand = values;
            }
            else
            {
                // Not really a multi value, just a single ValueExpression
                operand = values.Choices[0];
            }
        }

        // Construct the new criterium based on the parsed values
        return new Criterium { ParamName = name, Operator = type, Modifier = modifier, Operand = operand };
    }

    private string BuildValue()
    {
        // Turn ISNULL and NOTNULL operators into either true/or false to match the :missing modifier
        if (Operator == Operator.ISNULL)
            return "true";
        if (Operator == Operator.NOTNULL)
            return "false";

        if (Operand == null) throw new InvalidOperationException("Criterium does not have an operand");
        if (Operand is not ValueExpression) throw new FormatException("Expected a ValueExpression as operand");

        string value = Operand.ToString();

        if (Operator == Operator.EQ)
            return value;
        return OPERATOR_MAPPING.FirstOrDefault(t => t.Item2 == Operator)?.Item1 + value;
    }

    private static Tuple<Operator, string> findComparator(string value)
    {
        Tuple<string, Operator> opMap = OPERATOR_MAPPING.FirstOrDefault(t => value.StartsWith(t.Item1));

        return Tuple.Create(opMap.Item2, value.Substring(opMap.Item1.Length));
    }

    public Criterium Clone()
    {
        Criterium result = new()
        {
            Modifier = Modifier,
            Operand = Operand is Criterium criterium ? criterium.Clone() : Operand,
            Operator = Operator,
            ParamName = ParamName
        };

        return result;
    }
}
