/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/ewoutkramer/fhir-net-api/master/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Spark.Search.API.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Spark.Search.API.Search
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