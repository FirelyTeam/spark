/* 
 * Copyright (c) 2015-2018, Firely (info@fire.ly)
 * Copyright (c) 2020-2024, Incendi (info@incendi.no)
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Spark.Search.Support;
using System;

namespace Spark.Search
{
    /// <summary>
    /// DateTimeValue is allways specified up to the second.
    /// Spark uses it for the boundaries of a period. So fuzzy dates as in FhirDateTime (just year + month for example) get translated in an upper- and lowerbound in DateTimeValues.
    /// These are used for indexing.
    /// </summary>
    public class DateTimeValue : ValueExpression
    {
        public DateTimeOffset Value { get; private set; }

        public DateTimeValue(DateTimeOffset value)
        {
            // The DateValue datatype is not interested in any time related
            // components, so we must strip those off before converting to the string
            // value
            Value = value;
        }

        public DateTimeValue(string datetime)
        {
            if (!FhirDateTime.IsValidValue(datetime))
            {
                throw Error.Argument("datetime", "The string [" + datetime + "] cannot be translated to a DateTimeValue");
            }
            var fdt = new FhirDateTime(datetime);
            Value = fdt.ToDateTimeOffset(TimeSpan.Zero);
        }

        public override string ToString()
        {
            return new FhirDateTime(Value).ToString();
            //return Value.ToString("YYYY-MM-ddThh:mm:sszzz");
        }

        public static DateTimeValue Parse(string text)
        {
            return new DateTimeValue(text);
        }
    }
}