using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Logging;
using Spark.Engine.Model;
using Spark.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Engine.Search
{
    //This class is not static because it needs a IFhirModel to do some of the indexing (especially enums).
    public class ElementIndexer
    {
        private SparkEngineEventSource _log = SparkEngineEventSource.Log;
        private IFhirModel _fhirModel;

        public ElementIndexer(IFhirModel fhirModel)
        {
            _fhirModel = fhirModel;
        }

        private List<Expression> ListOf(params Expression[] args)
        {
            if (!args.Any())
                return null;

            var result = new List<Expression>();
            result.AddRange(args);
            return result;
        }

        private bool TestIfCodedEnum(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            bool? codedEnum = type.GenericTypeArguments?.FirstOrDefault()?.IsEnum;
            if (codedEnum.HasValue && codedEnum.Value)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Maps element to a list of Expression.
        /// </summary>
        /// <param name="element"></param>
        /// <returns>List of Expression, empty List if no mapping was possible.</returns>
        public List<Expression> Map(Element element)
        {
            var result = new List<Expression>();
            try
            {
                List<Expression> expressions = ToExpressions((dynamic)element);
                if (expressions != null)
                {
                    result.AddRange(expressions.Where(exp => exp != null).ToList());
                }
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
                _log.UnsupportedFeature("ElementIndexer.Map", "Mapping of type " + element.GetType().Name);
            }
            return result;
        }

        private List<Expression> ToExpressions(Markdown element)
        {
            if (element == null || String.IsNullOrWhiteSpace(element.Value))
                return null;

            return ListOf(new StringValue(element.Value));
        }
        private List<Expression> ToExpressions(Id element)
        {
            if (element == null || String.IsNullOrWhiteSpace(element.Value))
                return null;

            return ListOf(new StringValue(element.Value));
        }
        private List<Expression> ToExpressions(Oid element)
        {
            if (element == null || String.IsNullOrWhiteSpace(element.Value))
                return null;

            return ListOf(new StringValue(element.Value));
        }
        private List<Expression> ToExpressions(Integer element)
        {
            if (element == null || !element.Value.HasValue)
                return null;

            return ListOf(new NumberValue(element.Value.Value));
        }
        private List<Expression> ToExpressions(UnsignedInt element)
        {
            if (element == null || !element.Value.HasValue)
                return null;

            return ListOf(new NumberValue(element.Value.Value));
        }
        private List<Expression> ToExpressions(PositiveInt element)
        {
            if (element == null || !element.Value.HasValue)
                return null;

            return ListOf(new NumberValue(element.Value.Value));
        }
        private List<Expression> ToExpressions(Instant element)
        {
            if (element == null || !element.Value.HasValue)
                return null;

            var fdt = new FhirDateTime(element.Value.Value);
            return ToExpressions(fdt);
        }

        private List<Expression> ToExpressions(Time element)
        {
            if (element == null || String.Empty.Equals(element.Value))
                return null;

            return ListOf(new StringValue(element.Value));
        }
        private List<Expression> ToExpressions(FhirUri element)
        {
            if (element == null || String.Empty.Equals(element.Value))
                return null;

            return ListOf(new StringValue(element.Value));
        }
        private List<Expression> ToExpressions(Hl7.Fhir.Model.Date element)
        {
            if (element == null || String.Empty.Equals(element.Value))
                return null;

            FhirDateTime fdt = new FhirDateTime(element.Value);
            return ToExpressions(fdt);
        }

        private List<Expression> ToExpressions(FhirDecimal element)
        {
            if (element == null || !element.Value.HasValue)
                return null;

            return ListOf(new NumberValue(element.Value.Value));
        }

        /// <summary>
        /// { start : lowerbound-of-fhirdatetime, end : upperbound-of-fhirdatetime }
        /// <seealso cref="ToExpressions(Period)"/>, with lower and upper bounds of FhirDateTime as bounds of the Period.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private List<Expression> ToExpressions(FhirDateTime element)
        {
            if (element == null)
                return null;

            var bounds = new List<IndexValue>();

            bounds.Add(new IndexValue("start", new DateTimeValue(element.LowerBound())));
            bounds.Add(new IndexValue("end", new DateTimeValue(element.UpperBound())));

            return ListOf(new CompositeValue(bounds));
        }

        /// <summary>
        /// { start : lowerbound-of-period-start, end : upperbound-of-period-end }
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>

        private List<Expression> ToExpressions(Period element)
        {
            if (element == null || (element.StartElement == null && element.EndElement == null))
                return null;

            var bounds = new List<IndexValue>();

            if (element.StartElement != null)
                bounds.Add(new IndexValue("start", new DateTimeValue(element.StartElement.LowerBound())));
            if (element.EndElement != null)
                bounds.Add(new IndexValue("end", new DateTimeValue(element.EndElement.UpperBound())));

            return ListOf(new CompositeValue(bounds));
        }

        /// <summary>
        /// { system : system1, code: code1, text: display1 },
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private List<Expression> ToExpressions(Coding element)
        {
            if (element == null)
                return null;

            var values = new List<IndexValue>();
            if (element.Code != null)
                values.Add(new IndexValue("code", new StringValue(element.Code)));
            if (element.System != null)
                values.Add(new IndexValue("system", new StringValue(element.System)));
            if (element.Display != null)
                values.Add(new IndexValue("text", new StringValue(element.Display)));

            return ListOf(new CompositeValue(values));
        }

        /// <summary>
        /// { code : identifier-value, system : identifier-system, text : identifier-type }
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private List<Expression> ToExpressions(Identifier element)
        {
            if (element == null)
                return null;

            var values = new List<IndexValue>();
            if (element.Value != null)
                values.Add(new IndexValue("code", new StringValue(element.Value)));
            if (element.System != null)
                values.Add(new IndexValue("system", new StringValue(element.System)));
            if (element.Type != null)
                values.Add(new IndexValue("text", new StringValue(element.Type.Text)));

            return ListOf(new CompositeValue(values));
        }

        /// <summary>
        /// [
        ///     { system : system1, code: code1, text: display1 },
        ///     { system : system2, code: code2, text: display2 },
        ///     text : codeableconcept-text
        /// ]
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private List<Expression> ToExpressions(CodeableConcept element)
        {
            if (element == null)
                return null;

            var result = new List<Expression>();
            if (element.Coding != null && element.Coding.Any())
            {
                result.AddRange(element.Coding.SelectMany(c => ToExpressions(c)));
            }
            if (element.Text != null)
            {
                result.Add(new IndexValue("text", new StringValue(element.Text)));
            }
            return result;
        }

        /// <summary>
        /// { code : contactpoint-value, system : contactpoint-use }
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private List<Expression> ToExpressions(ContactPoint element)
        {
            if (element == null)
                return null;

            var values = new List<IndexValue>();
            if (element.Value != null)
                values.Add(new IndexValue("code", Map(element.ValueElement)));
            if (element.System != null)
                values.Add(new IndexValue("system", Map(element.SystemElement)));
            if (element.Use != null)
                values.Add(new IndexValue("use", Map(element.UseElement)));

            return ListOf(new CompositeValue(values));
        }

        /// <summary>
        /// { code : true/false }, system is absent
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private List<Expression> ToExpressions(FhirBoolean element)
        {
            if (element == null || !element.Value.HasValue)
                return null;

            var values = new List<IndexValue>();
            values.Add(new IndexValue("code", element.Value.Value ? new StringValue(Boolean.TrueString) : new StringValue(Boolean.FalseString)));

            return ListOf(new CompositeValue(values));

            //TODO: Include implied system: http://hl7.org/fhir/special-values ? 
        }

        private List<Expression> ToExpressions(ResourceReference element)
        {
            if (element == null || element.Url == null)
                return null;

            Expression value = null;
            var uri = element.Url;
            if (uri.IsAbsoluteUri)
            {
                //This is a fully specified url, either internal or external. Don't change it.
                value = new StringValue(uri.ToString());
            }
            else if (uri.ToString().StartsWith("#"))
            {
                //TODO: This is a reference to a contained resource in the same bundle. Should we index it at all?
            }
            else
            {
                //This is a relative url, so it is meant to point to something internal to our server.
                value = new StringValue(uri.ToString());
                //TODO: expand to absolute url with Localhost?
            }

            return ListOf(value);

        }

        /// <summary>
        /// Returns list of all Address elements
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private List<Expression> ToExpressions(Address element)
        {
            if (element == null)
                return null;

            var values = new List<Expression>();
            values.Add(element.City != null ? new StringValue(element.City) : null);
            values.Add(element.Country != null ? new StringValue(element.Country) : null);
            values.AddRange(element.Line != null ? element.Line.Select(line => new StringValue(line)) : null);
            values.Add(element.State != null ? new StringValue(element.State) : null);
            values.Add(element.Text != null ? new StringValue(element.Text) : null);
            values.Add(element.Use.HasValue ? new StringValue(_fhirModel.GetLiteralForEnum(element.Use.Value)) : null);
            values.Add(element.PostalCode != null ? new StringValue(element.PostalCode) : null);

            return values.Where(v => v != null).ToList();
        }

        /// <summary>
        /// Returns list of Given and Family parts of the name
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private List<Expression> ToExpressions(HumanName element)
        {
            if (element == null)
                return null;

            var values = new List<Expression>();
            values.AddRange(ToExpressions(element.GivenElement));
            values.AddRange(ToExpressions(element.FamilyElement));

            return values;
        }

        private List<Expression> ToExpressions(Quantity element)
        {
            try
            {
                return element != null ? ListOf(element.ToExpression()) : null;
            }
            catch (ArgumentException ex)
            {
                _log.InvalidElement("unknown", String.Format("Quantity: {0} {1} {2}", element.Code, element.Unit, element.Value), ex.Message);
            }
            return null;
        }

        private List<Expression> ToExpressions(Code element)
        {
            return element != null ? ListOf(new StringValue(element.Value)) : null;
        }

        private List<Expression> ToExpressions(FhirString element)
        {
            return element != null ? ListOf(new StringValue(element.Value)) : null;
        }

        private List<Expression> ToExpressions(IEnumerable<Element> elements)
        {
            if (elements == null)
                return null;

            return elements.SelectMany(el => Map(el)).ToList();
        }

        private List<Expression> ToExpressions<T>(Code<T> element) where T : struct
        {
            if (element != null && element.Value.HasValue)
            {
                var values = new List<IndexValue>();
                values.Add(new IndexValue("code", new StringValue(_fhirModel.GetLiteralForEnum((element.Value.Value as Enum)))));
                return ListOf(new CompositeValue(values));
            }

            return null;
        }
    }
}

