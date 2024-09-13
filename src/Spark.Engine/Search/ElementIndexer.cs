/* 
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Microsoft.CSharp.RuntimeBinder;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Logging;
using Spark.Engine.Model;
using Spark.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using Expression = Spark.Search.Expression;

namespace Spark.Engine.Search;

public class ElementIndexer
{
    private readonly SparkEngineEventSource _log = SparkEngineEventSource.Log;
    private readonly IFhirModel _fhirModel;
    private readonly IReferenceNormalizationService _referenceNormalizationService;

    public ElementIndexer(IFhirModel fhirModel, IReferenceNormalizationService referenceNormalizationService = null)
    {
        _fhirModel = fhirModel;
        _referenceNormalizationService = referenceNormalizationService;
    }

    private List<Expression> ListOf(params Expression[] args)
    {
        if (!args.Any())
            return null;

        var result = new List<Expression>();
        result.AddRange(args);
        return result;
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
            // TODO: How to handle composite SearchParameter type
            //if (element is Sequence.VariantComponent) return result;
            List<Expression> expressions = ToExpressions((dynamic)element);
            if (expressions != null)
            {
                result.AddRange(expressions.Where(exp => exp != null).ToList());
            }
        }
        catch (RuntimeBinderException)
        {
            _log.UnsupportedFeature("ElementIndexer.Map", "Mapping of type " + element.GetType().Name);
        }
        return result;
    }

    private List<Expression> ToExpressions(Age element)
    {
        if (element == null) return null;

        return ToExpressions(element as Quantity);
    }

    private List<Expression> ToExpressions(Location.PositionComponent element)
    {
        if (element == null || (element.Latitude == null && element.Longitude == null))
            return null;

        var position = new List<IndexValue>
        {
            new IndexValue("latitude", new NumberValue(element.Latitude.Value)),
            new IndexValue("longitude", new NumberValue(element.Longitude.Value))
        };

        return ListOf(new CompositeValue(position));
    }

    private List<Expression> ToExpressions(Base64Binary element)
    {
        if (element == null || element.Value == null || element.Value.Length == 0)
            return null;

        return ToExpressions(new FhirString(element.ToString()));
    }

    private List<Expression> ToExpressions(Attachment element)
    {
        if (element == null || element.UrlElement == null)
            return null;

        return ToExpressions(element.UrlElement);
    }

    private List<Expression> ToExpressions(Timing element)
    {
        if (element == null || element.Repeat == null || element.Repeat.Bounds == null)
            return null;
            
        // TODO: Should I handle Duration?
        return ToExpressions(element.Repeat.Bounds as Period);
    }

    private List<Expression> ToExpressions(Extension element)
    {
        if (element == null)
            return null;

        return ToExpressions((dynamic) element.Value);
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
    private List<Expression> ToExpressions(FhirUrl element)
    {
        if (element == null || String.Empty.Equals(element.Value))
            return null;

        return ListOf(new StringValue(element.Value));
    }
    private List<Expression> ToExpressions(FhirUri element)
    {
        if (element == null || String.Empty.Equals(element.Value))
            return null;

        return ListOf(new StringValue(UriUtil.NormalizeUri(element.Value)));
    }

    private List<Expression> ToExpressions(Canonical element)
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

        var bounds = new List<IndexValue>
        {
            new IndexValue("start", new DateTimeValue(element.LowerBound())),
            new IndexValue("end", new DateTimeValue(element.UpperBound()))
        };

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

        var values = new List<IndexValue>
        {
            new IndexValue("code", element.Value.Value ? new StringValue("true") : new StringValue("false"))
        };

        return ListOf(new CompositeValue(values));
    }

    private List<Expression> ToExpressions(ResourceReference element)
    {
        if (element == null)
            return null;

        if (element.Url != null)
        {
            Expression value = null;
            var uri = element.Url;
            if (uri.IsAbsoluteUri)
            {
                //This is a fully specified url, either internal or external. Don't change it.
                var stringValue = new StringValue(uri.ToString());

                // normalize reference value to be able to use normalized criteria for search.
                // https://github.com/FirelyTeam/spark/issues/35 
                value = _referenceNormalizationService != null
                    ? _referenceNormalizationService.GetNormalizedReferenceValue(stringValue, null)
                    : stringValue;
            }
            else
            {
                //This is a relative url, so it is meant to point to something internal to our server.
                value = new StringValue(uri.ToString());
                //TODO: expand to absolute url with Localhost?
            }

            return ListOf(value);
        }
        else if (element.Identifier != null)
        {
            return ToExpressions(element.Identifier);
        }

        return null;
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

        var values = new List<Expression>
        {
            element.City != null ? new StringValue(element.City) : null,
            element.Country != null ? new StringValue(element.Country) : null,
            element.State != null ? new StringValue(element.State) : null,
            element.Text != null ? new StringValue(element.Text) : null,
            element.Use.HasValue ? new StringValue(_fhirModel.GetLiteralForEnum(element.Use.Value)) : null,
            element.PostalCode != null ? new StringValue(element.PostalCode) : null,
        };
        values.AddRange(element.Line?.Select(line => new StringValue(line)));

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
        if (element.FamilyElement != null)
            values.AddRange(ToExpressions(element.FamilyElement));
        if (element.PrefixElement != null && element.PrefixElement.Count > 0)
            values.AddRange(ToExpressions(element.PrefixElement));
        if (element.SuffixElement != null && element.SuffixElement.Count > 0)
            values.AddRange(ToExpressions(element.SuffixElement));
        if (element.TextElement != null)
            values.AddRange(ToExpressions(element.TextElement));

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

    private List<Expression> ToExpressions<T>(Code<T> element) where T : struct, Enum
    {
        if (element != null && element.Value.HasValue)
        {
            var values = new List<IndexValue>
            {
                new IndexValue("code", new StringValue(_fhirModel.GetLiteralForEnum((element.Value.Value as Enum))))
            };

            return ListOf(new CompositeValue(values));
        }

        return null;
    }
}