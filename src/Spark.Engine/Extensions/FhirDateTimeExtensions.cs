/*
 * Copyright (c) 2015, Firely (info@fire.ly) and contributors
 * Copyright (c) 2020-2023, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using System;

namespace Spark.Engine.Extensions
{
    public static class FhirDateTimeExtensions
    {
        public enum FhirDateTimePrecision
        {
            Year = 4,       //1994
            Month = 7,      //1994-10
            Day = 10,       //1994-10-21
            Minute = 15,    //1994-10-21T13:45
            Second = 18    //1994-10-21T13:45:21
        }

        public static FhirDateTimePrecision Precision(this FhirDateTime fdt)
        {
            return (FhirDateTimePrecision)Math.Min(fdt.Value.Length, 18); //Ignore timezone for stating precision.
        }

        public static DateTimeOffset LowerBound(this FhirDateTime fdt)
        {
            return fdt.ToDateTimeOffset(TimeSpan.Zero);
        }

        public static DateTimeOffset UpperBound(this FhirDateTime fdt)
        {
            var start = fdt.LowerBound();
            var end = (fdt.Precision()) switch
            {
                FhirDateTimePrecision.Year => start.AddYears(1),
                FhirDateTimePrecision.Month => start.AddMonths(1),
                FhirDateTimePrecision.Day => start.AddDays(1),
                FhirDateTimePrecision.Minute => start.AddMinutes(1),
                FhirDateTimePrecision.Second => start.AddSeconds(1),
                _ => start
            };
            return end;
        }
    }
}
