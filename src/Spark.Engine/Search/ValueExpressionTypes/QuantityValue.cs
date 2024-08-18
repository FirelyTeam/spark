/* 
 * Copyright (c) 2015-2018, Firely (info@fire.ly) and contributors
 * Copyright (c) 2021-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Spark.Search.Support;
using Hl7.Fhir.Serialization;
using System;

namespace Spark.Search
{
    public class QuantityValue : ValueExpression
    {
        public decimal Number { get; private set; }

        public string Namespace { get; private set; }

        public string Unit { get; private set; }

        public QuantityValue(decimal number, string unit)
        {
            Number = number;
            Unit = unit;
        }

        public QuantityValue(decimal number, string ns, string unit)
        {
            Number = number;
            Unit = unit;
            Namespace = ns;
        }

        public override string ToString()
        {
            var ns = Namespace ?? string.Empty;
            return PrimitiveTypeConverter.ConvertTo<string>(Number) + "|" +
                StringValue.EscapeString(ns) + "|" +
                StringValue.EscapeString(Unit);
        }

        public static QuantityValue Parse(string text)
        {
            if (text == null) throw Error.ArgumentNull("text");

            string[] triple = text.SplitNotEscaped('|');

            if (triple.Length != 3)
                throw Error.Argument("text", "Quantity needs to have three parts separated by '|'");

            if(triple[0] == string.Empty) 
                throw new FormatException("Quantity needs to specify a number");

            var number = PrimitiveTypeConverter.ConvertTo<Decimal>(triple[0]);
            var ns = triple[1] != string.Empty ? StringValue.UnescapeString(triple[1]) : null;

            if (triple[2] == string.Empty)
                throw new FormatException("Quantity needs to specify a unit");

            var unit = StringValue.UnescapeString(triple[2]);
 
            return new QuantityValue(number,ns,unit);
        }     
    }
}