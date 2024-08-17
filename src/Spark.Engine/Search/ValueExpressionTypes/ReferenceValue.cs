/* 
 * Copyright (c) 2015-2018, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using Spark.Search.Support;
using System;

namespace Spark.Search
{
    public class ReferenceValue : ValueExpression
    {
        public string Value { get; private set; }
     
        public ReferenceValue(string value)
        {
            if (!Uri.IsWellFormedUriString(value, UriKind.Absolute) &&
                !Id.IsValidValue(value))
                throw Error.Argument("text", "Reference is not a valid Id nor a valid absolute Url");

            Value = value;
        }
                              
        public override string ToString()
        {
            return StringValue.EscapeString(Value);
        }

        public static ReferenceValue Parse(string text)
        {
            var value = StringValue.UnescapeString(text);
         
            return new ReferenceValue(value);
        }
    }
}