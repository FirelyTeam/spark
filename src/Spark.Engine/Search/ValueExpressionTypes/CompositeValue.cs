/* 
 * Copyright (c) 2015-2018, Firely (info@fire.ly)
 * Copyright (c) 2021-2024, Incendi (info@incendi.no)
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Search.Support;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Search
{
    public class CompositeValue : ValueExpression
    {
        private const char TUPLESEPARATOR = '$';

        public ValueExpression[] Components { get; private set; }

        public CompositeValue(ValueExpression[] components)
        {
            if (components == null) throw Error.ArgumentNull("components");

            Components = components;
        }

        public CompositeValue(IEnumerable<ValueExpression> components)
        {
            if (components == null) throw Error.ArgumentNull("components");

            Components = components.ToArray();
        }

        public override string ToString()
        {
            var values = Components.Select(v => v.ToString());
            return string.Join(TUPLESEPARATOR.ToString(),values);
        }


        public static CompositeValue Parse(string text)
        {
            if (text == null) throw Error.ArgumentNull("text");

            var values = text.SplitNotEscaped(TUPLESEPARATOR);

            return new CompositeValue(values.Select(v => new UntypedValue(v)));
        }
    }
}