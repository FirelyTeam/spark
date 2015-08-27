/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */
using System;
using System.Collections.Generic;
using Fhir.Metrics;
using MongoDB.Bson;
using Model = Hl7.Fhir.Model;

namespace Spark.Search.Mongo
{
    public static class UnitsOfMeasureHelper
    {
        static Uri UcumUri = new Uri("http://unitsofmeasure.org");
        static SystemOfUnits system = UCUM.Load();

        public static Quantity ToUnitsOfMeasureQuantity(this Model.Quantity input)
        {
            Metric metric = (input.Code != null) ? system.Metric(input.Code) : new Metric(new List<Metric.Axis>());
            Exponential value = input.Value ?? 1; //todo: is this assumption correct?
            return new Quantity(value, metric);
        }
        public static Model.Quantity ToFhirModelQuantity(this Quantity input)
        {
            Model.Quantity output = new Model.Quantity();
            output.Value = (decimal)input.Value;
            output.Code = input.Metric.ToString();
            output.Units = output.Code;
            output.System = UCUM.Uri.ToString();
            return output;
        }

        public static BsonDocument ToBson(this Quantity quantity)
        {
            quantity = system.Canonical(quantity);
            string searchable = quantity.LeftSearchableString();

            BsonDocument block = new BsonDocument
            {
                { "system", UCUM.Uri.ToString() },
                { "value", quantity.GetValueAsBson() },
                { "decimals", searchable },
                { "unit", quantity.Metric.ToString() }
            };
            return block;
        }

        public static BsonDocument NonUcumIndexed(this Model.Quantity quantity)
        {
            string system = (quantity.System != null) ? quantity.System.ToString() : null;
            BsonDocument block = new BsonDocument
            {
                { "system", system },
                { "value", quantity.GetValueAsBson() },
                { "unit", quantity.Code }
            };
            return block;
        }   

        public static BsonDocument ToBson(this Model.Quantity quantity)
        {
            if (quantity.IsUcum())
            {
                Quantity q = quantity.ToUnitsOfMeasureQuantity();
                return q.ToBson();
            }
            else return quantity.NonUcumIndexed();
        }

        public static bool IsUcum(this Model.Quantity quantity)
        {
            if (quantity.System == null)
                return false;

            return UcumUri.IsBaseOf(new Uri(quantity.System));
        }

        public static Quantity Canonical(this Quantity input)
        {
            return system.Canonical(input);
        }

        public static Model.Quantity Canonical(this Model.Quantity input)
        {
            if (IsUcum(input))
            {
                Quantity quantity = input.ToUnitsOfMeasureQuantity();
                quantity = system.Canonical(quantity);
                return quantity.ToFhirModelQuantity();
            }
            else return input;
        }
        
        public static string ValueAsSearchableString(this Model.Quantity quantity)
        {
            Quantity q = quantity.ToUnitsOfMeasureQuantity();
            return q.LeftSearchableString();
        }

        public static string SearchableString(Quantity quantity)
        {
            return quantity.LeftSearchableString(); // extension access
        }

        public static BsonDouble GetValueAsBson(this Model.Quantity quantity)
        {
            double value = (double)quantity.Value;
            return new BsonDouble(value);
        }

        public static BsonDouble GetValueAsBson(this Quantity quantity)
        {
            double value = (double)quantity.Value.ToDecimal();
            return new BsonDouble(value);
        }

        // This code might have a better place somewhere else: //mh
        public static Model.Quantity ToModelQuantity(this ValueExpression expression)
        {
            QuantityValue q = QuantityValue.Parse(expression.ToString());            
            Model.Quantity quantity = new Model.Quantity
            {                
                Value = q.Number,
                System = q.Namespace,
                Units = q.Unit,
                Code = q.Unit
            };
            return quantity;
        }
    }

    
}
