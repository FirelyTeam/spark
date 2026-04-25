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

public class CompositeValue : ValueExpression
{
    private const char TUPLE_SEPARATOR = '$';

    public CompositeValue(ValueExpression[] components)
    {
        Components = components ?? throw Error.ArgumentNull("components");
    }

    public CompositeValue(IEnumerable<ValueExpression> components)
    {
        if (components == null) throw Error.ArgumentNull("components");

        Components = components == null
            ? throw Error.ArgumentNull("components")
            : components.ToArray();
    }

    public ValueExpression[] Components { get; }

    public override string ToString()
    {
        IEnumerable<string> values = Components.Select(v => v.ToString());
        return string.Join(TUPLE_SEPARATOR.ToString(), values);
    }

    public static CompositeValue Parse(string text)
    {
        if (text == null) throw Error.ArgumentNull("text");

        string[] values = text.SplitNotEscaped(TUPLE_SEPARATOR);

        return new CompositeValue(values.Select(v => new UntypedValue(v)));
    }
}
