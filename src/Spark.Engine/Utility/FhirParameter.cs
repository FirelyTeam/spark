using Hl7.Fhir.Serialization;
using System;

namespace Spark.Engine.Utility
{
    public static class FhirParameterParser
    {
        public static DateTimeOffset? ParseDateParameter(string value)
        {
            return DateTimeOffset.TryParse(value, out var dateTime)
                ? dateTime : (DateTimeOffset?)null;
        }

        public static int? ParseIntParameter(string value)
        {
            return (int.TryParse(value, out int n)) ? n : default(int?);
        }

        public static bool? ParseBoolParameter(string value)
        {
            if (value == null) return null;
            try
            {
                bool b = PrimitiveTypeConverter.ConvertTo<bool>(value);
                return (bool.TryParse(value, out b)) ? b : default(bool?);
            }
            catch
            {
                return null;
            }
        }
    }
}
