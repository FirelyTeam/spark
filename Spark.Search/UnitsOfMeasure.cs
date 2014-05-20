/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Search
{
    public static class Units
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
        
        ///<summary>
        /// Creates a string from a decimal that allows compare-from-left string searching 
        /// for finding values that fall within a the precision of a given string representing a decimal .
        ///</summary>
        public static string DecimalSearchable(decimal value)
        {
            string s = Convert.ToString(value, new CultureInfo("en-US"));
            StringBuilder b = new StringBuilder(s);

            int reminder = 0;

            for (int i = b.Length-1; i >= 0; i--)
            {
                if (b[i] == '.') continue;
                int n = (int)Char.GetNumericValue(b[i]);
                n += reminder;
                
                reminder = n / 10;
                n = n % 10;
                reminder += (n > 5) ? 1 : 0;
                char c = Convert.ToString(n)[0];
                b[i] = c;

            }
            return b.ToString();
        }

        public static string GetDecimalSearchableValue(this Quantity quantity)
        {
            return DecimalSearchable(quantity.Value ?? 0);
        }

        public static BsonDouble GetValueAsBson(this Quantity quantity)
        {
            double value = (double)quantity.Value;
            return new BsonDouble(value);
        }
    
    }

    
}
