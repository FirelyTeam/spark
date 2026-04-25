/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Engine.Search.Support;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Engine.Search.Types;

public class ChoiceValue : ValueExpression
{
    private const char VALUE_SEPARATOR = ',';

    public ChoiceValue(ValueExpression[] choices)
    {
        Choices = choices ?? throw Error.ArgumentNull("choices");
    }

    public ChoiceValue(IEnumerable<ValueExpression> choices)
    {
        Choices = choices == null
            ? throw Error.ArgumentNull("choices")
            : choices.ToArray();
    }

    public ValueExpression[] Choices { get; }

    public override string ToString()
    {
        IEnumerable<string> values = Choices.Select(v => v.ToString());
        return string.Join(VALUE_SEPARATOR.ToString(), values);
    }

    public static ChoiceValue Parse(string text)
    {
        if (text == null)
            Error.ArgumentNull("text");

        string[] values = text.SplitNotEscaped(VALUE_SEPARATOR);

        return new ChoiceValue(values.Select(splitIntoComposite));
    }

    private static ValueExpression splitIntoComposite(string text)
    {
        CompositeValue composite = CompositeValue.Parse(text);

        // If there's only one component, this really was a single value
        return composite.Components.Length == 1
            ? composite.Components[0]
            : composite;
    }
}
