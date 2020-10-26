/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/ewoutkramer/fhir-net-api/master/LICENSE
 */


namespace Spark.Search
{
    public abstract class Expression
    {
    }

    public abstract class ValueExpression : Expression
    {
        public string ToUnescapedString()
        {
            var value = this;
            if (value is UntypedValue untyped)
            {
                value = untyped.AsStringValue();

                return StringValue.UnescapeString(value.ToString());
            }
            return value.ToString();
        }
    }
}
