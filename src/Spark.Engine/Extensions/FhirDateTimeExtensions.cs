using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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


        [Obsolete]
        public static Period ToPeriod(this FhirDateTime fdt)
        {
            var result = new Period();
            var dtoStart = fdt.ToDateTimeOffset();
            result.StartElement = new FhirDateTime(dtoStart);
            var dtoEnd = dtoStart;

            switch (fdt.Precision())
            {
                case FhirDateTimePrecision.Year: dtoEnd = dtoStart.AddYears(1); break;
                case FhirDateTimePrecision.Month: dtoEnd = dtoStart.AddMonths(1); break;
                case FhirDateTimePrecision.Day: dtoEnd = dtoStart.AddDays(1); break;
                case FhirDateTimePrecision.Minute: dtoEnd = dtoStart.AddMinutes(1); break;
                case FhirDateTimePrecision.Second: dtoEnd = dtoStart.AddSeconds(1); break;
                default: dtoEnd = dtoStart; break;
            }

            result.EndElement = new FhirDateTime(dtoEnd);
            return result;
        }

        public static DateTimeOffset LowerBound(this FhirDateTime fdt)
        {
            return fdt.ToDateTimeOffset();
        }

        public static DateTimeOffset UpperBound(this FhirDateTime fdt)
        {
            var dtoStart = fdt.LowerBound();
            var dtoEnd = dtoStart;
            switch (fdt.Precision())
            {
                case FhirDateTimePrecision.Year: dtoEnd = dtoStart.AddYears(1); break;
                case FhirDateTimePrecision.Month: dtoEnd = dtoStart.AddMonths(1); break;
                case FhirDateTimePrecision.Day: dtoEnd = dtoStart.AddDays(1); break;
                case FhirDateTimePrecision.Minute: dtoEnd = dtoStart.AddMinutes(1); break;
                case FhirDateTimePrecision.Second: dtoEnd = dtoStart.AddSeconds(1); break;
                default: dtoEnd = dtoStart; break;
            }

            return dtoEnd;
        }
    }
}
