using Hl7.Fhir.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Spark.Engine.Utility
{
    public static class FhirParameterParser
    {
        public static DateTimeOffset? ParseDateParameter(string value)
        {
            return DateTimeOffset.Parse(value);
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
