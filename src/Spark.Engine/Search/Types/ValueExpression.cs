/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2020-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

namespace Spark.Engine.Search.Types;

public abstract class ValueExpression : Expression
{
    public string ToUnescapedString()
    {
        ValueExpression value = this;
        if (value is not UntypedValue untyped)
            return value.ToString();

        value = untyped.AsStringValue();
        return StringValue.UnescapeString(value.ToString());
    }
}
