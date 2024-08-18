/* 
 * Copyright (c) 2015-2018, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Serialization;
using System;

namespace Spark.Search
{
    public class NumberValue : ValueExpression
    {
        public Decimal Value { get; private set; }
     
        public NumberValue(Decimal value)
        {
            Value = value;
        }
                              
        public override string ToString()
        {
            return PrimitiveTypeConverter.ConvertTo<string>(Value);
        }

        public static NumberValue Parse(string text)
        {
            return new NumberValue(PrimitiveTypeConverter.ConvertTo<Decimal>(text));
        }
    }
}