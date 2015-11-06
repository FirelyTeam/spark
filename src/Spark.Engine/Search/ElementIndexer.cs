using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Model;
using Spark.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Engine.Search
{
    //This class is not static because it needs a IFhirModel to do some of the indexing (especially enums).
    public class ElementIndexer
    {
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

        public List<Expression> ToExpressions(Element element)
        {
            return element == null ? null : ListOf(new StringValue(element.ToString()));
        }

        public List<Expression> ToExpressions(FhirDecimal element)
        {
            if (element == null || !element.Value.HasValue)
                return null;

            return ListOf(new NumberValue(element.Value.Value));
        }

        public List<Expression> ToExpressions(FhirDateTime element)
        {
            if (element == null)
                return null;

            var bounds = new List<IndexValue>();

            bounds.Add(new IndexValue("start", new DateValue(element.LowerBound())));
            bounds.Add(new IndexValue("end", new DateValue(element.UpperBound())));

            return ListOf(new CompositeValue(bounds));
        }

        public List<Expression> ToExpressions(Period element)
        {
            if (element == null || (element.StartElement == null && element.EndElement == null))
                return null;

            var bounds = new List<IndexValue>();

            if (element.StartElement != null)
                bounds.Add(new IndexValue("start", new DateValue(element.StartElement.LowerBound())));
            if (element.EndElement != null)
                bounds.Add(new IndexValue("end", new DateValue(element.EndElement.UpperBound())));

            return ListOf(new CompositeValue(bounds));
        }

        public List<Expression> ToExpressions(Coding element)
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

        public List<Expression> ToExpressions(Identifier element)
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

        public List<Expression> ToExpressions(CodeableConcept element)
        {
            if (element == null)
                return null;

            var result = new List<Expression>();

            result.AddRange(element.Coding.SelectMany(c => ToExpressions(c)));

            if (element.Text != null)
                result.Add(new IndexValue("text", new StringValue(element.Text)));

            return result;
        }

        public List<Expression> ToExpressions(ContactPoint element)
        {
            if (element == null)
                return null;

            var values = new List<IndexValue>();
            if (element.Value != null)
                values.Add(new IndexValue("code", new StringValue(element.Value)));
            if (element.Use != null)
                values.Add(new IndexValue("system", new StringValue(Enum.GetName(typeof(ContactPoint.ContactPointUse), element.Use))));

            return ListOf(new CompositeValue(values));
        }

        public List<Expression> ToExpressions(FhirBoolean element)
        {
            if (element == null || !element.Value.HasValue)
                return null;

            return ListOf(new IndexValue("code", element.Value.Value ? new StringValue(Boolean.TrueString) : new StringValue(Boolean.FalseString)));
        }

        public List<Expression> ToExpressions(ResourceReference element)
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

        public List<Expression> ToExpressions(Address element)
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

            return values;
        }

        public List<Expression> ToExpressions(HumanName element)
        {
            if (element == null)
                return null;

            var values = new List<Expression>();
            values.AddRange(ToExpressions(element.GivenElement));
            values.AddRange(ToExpressions(element.FamilyElement));

            return values;
        }

        public List<Expression> ToExpressions(Quantity element)
        {
            return element != null ? ListOf(element.ToExpression()) : null;
        }

        public List<Expression> ToExpressions(Code element)
        {
            return element != null ? ListOf(new StringValue(element.Value)) : null;
        }

        public List<Expression> ToExpressions(FhirString element)
        {
            return element != null ? ListOf(new StringValue(element.Value)) : null;
        }

        public List<Expression> ToExpressions(IEnumerable<Element> elements)
        {
            if (elements == null)
                return null;

            return elements.SelectMany(el => ToExpressions(el)).ToList();
        }
    }
}

