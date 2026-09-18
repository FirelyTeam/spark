/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Engine.Search.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Engine.Model;

public class IndexValue : ValueExpression
{
    // FIXME: [next-major-release] Change this to a simpler type like Expression[].
    private readonly List<Expression> _values;

    [Obsolete("This constructor will be removed in the next release, use one of the other constructors instead.")]
    public IndexValue()
    {
        _values = [];
    }

    public IndexValue(string name)
    {
        Name = name;
        _values = [];
    }

    public IndexValue(string name, List<Expression> values): this(name)
    {
        Values = values;
    }

    public IndexValue(string name, params Expression[] values): this(name)
    {
        Values = [.. values];
    }

    public string Name { get; set; }

    // FIXME: [next-major-release] Return a simpler type like Expression[].
    public List<Expression> Values
    {
        get { return _values; }
        // FIXME: [next-major-release] Change this to init.
        set { _values.AddRange(value); }
    }

    // FIXME: [next-major-release] Remove this.
    [Obsolete("AddValue(Expression) will be removed in the next release.")]
    public void AddValue(Expression value)
    {
        _values.Add(value);
    }
}

public static class IndexValueExtensions
{
    public static IEnumerable<IndexValue> IndexValues(this IndexValue root)
    {
        return root.Values.OfType<IndexValue>();
    }
}
