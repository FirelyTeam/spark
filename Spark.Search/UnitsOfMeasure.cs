using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Search
{
    public static class UnitsOfMeasure
    {
        public static Quantity Standardize(this Quantity quantity)
        {
            Quantity result = new Quantity();

            // Example code 
            if (quantity.Units == "mg") 
            {
                result.Units = "g";
                result.System = quantity.System;
                result.Value = quantity.Value / 1000;
            }
            else
            {
                result.Units = quantity.Units;
                result.System = quantity.System;
                result.Value = quantity.Value;
            }
            return result;
        }
        
        public static string GetStandardizedValue(this Quantity quantity)
        {
            return Convert.ToString(quantity.Value, new CultureInfo("en-US"));
        }
    
    }

    
}
