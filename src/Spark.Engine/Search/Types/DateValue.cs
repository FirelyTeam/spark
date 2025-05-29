/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2020-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Spark.Engine.Search.Support;
using System;

namespace Spark.Engine.Search.Types;

public class DateValue : ValueExpression
{
    public DateValue(DateTimeOffset value)
    {
        // The DateValue datatype is not interested in any time related
        // components, so we must strip those off before converting to the string
        // value
        Value = value.Date.ToString("yyyy-MM-dd");
    }

    public DateValue(string date)
    {
        if (!Date.IsValidValue(date))
        {
            if (!FhirDateTime.IsValidValue(date))
                throw Error.Argument("date",
                    $"The string [{date}] is not a valid FHIR date string and isn't a FHIR datetime either");

            // This was a time, so we can just use the date portion of this
            date = new FhirDateTime(date).ToDateTimeOffset(TimeSpan.Zero).Date.ToString("yyyy-MM-dd");
        }

        Value = date;
    }

    public string Value { get; }

    public override string ToString() => Value;

    public static DateValue Parse(string text) => new(text);
}
