/* 
 * Copyright (c) 2015-2018, Firely (info@fire.ly) and contributors
 * Copyright (c) 2021-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Search.Support;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Search
{
    public class ChoiceValue : ValueExpression
    {
        private const char VALUESEPARATOR = ',';

        public ValueExpression[]  Choices { get; private set; }

        public ChoiceValue(ValueExpression[] choices)
        {
            if (choices == null) Error.ArgumentNull("choices");

            Choices = choices;
        }

        public ChoiceValue(IEnumerable<ValueExpression> choices)
        {
            if (choices == null) Error.ArgumentNull("choices");

            Choices = choices.ToArray();
        }

        public override string ToString()
        {
            var values = Choices.Select(v => v.ToString());
            return string.Join(VALUESEPARATOR.ToString(),values);
        }

        public static ChoiceValue Parse(string text)
        {
            if (text == null) Error.ArgumentNull("text");

            var values = text.SplitNotEscaped(VALUESEPARATOR);

            return new ChoiceValue(values.Select(v => splitIntoComposite(v)));
        }

        private static ValueExpression splitIntoComposite(string text)
        {
            var composite = CompositeValue.Parse(text);

            // If there's only one component, this really was a single value
            if (composite.Components.Length == 1)
                return composite.Components[0];
            else
                return composite;
        }
    }
}