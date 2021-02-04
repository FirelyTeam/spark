﻿using System;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.FhirPath;
using Hl7.Fhir.Model;
using Hl7.FhirPath;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    /// <summary>
    /// FIXME: the aim of this extension is to fix a single issue in the original ElementNavFhirExtensions.Select.
    /// The issue happens when it's used together with some expression selecting property of type Date, but the
    /// underlying value is of PrimitiveType, say, "2020-12-10".
    ///
    /// Cause of this: it doesn't support Hl7.Fhir.ElementModel.Types.Date type (only Hl7.Fhir.ElementModel.Types.DateTime is originally supported).
    ///
    /// Test failing: X012_Goal.
    ///
    /// TODO: create PR in the original repo and fix it.
    /// 
    /// </summary>

    internal static class ElementNavFhirExtensionsNew
    {
        public static IEnumerable<Base> SelectNew(
            this Base input,
            string expression,
            FhirEvaluationContext ctx = null)
        {
            var inputNav = input.ToTypedElement();
            var result = inputNav.Select(expression, ctx ?? FhirEvaluationContext.CreateDefault());
            return result.ToFhirValues();
        }

        public static IEnumerable<Base> ToFhirValues(this IEnumerable<ITypedElement> results)
        {
            return results.Select(r =>
            {
                if (r == null)
                {
                    return null;
                }
                var fhirValueProvider = r.Annotation<IFhirValueProvider>();
                if (fhirValueProvider != null)
                {
                    return fhirValueProvider.FhirValue;
                }
                var obj = r.Value;
                switch (obj)
                {
                    case bool flag:
                        return new FhirBoolean(flag);
                    case long num:
                        return new Integer((int)num);
                    case decimal num:
                        return new FhirDecimal(num);
                    case string s:
                        return new FhirString(s);
                    case Hl7.Fhir.ElementModel.Types.Date date:
                        return new Date(date.ToString());
                    case Hl7.Fhir.ElementModel.Types.DateTime dateTime:
                        return new FhirDateTime(dateTime.ToDateTimeOffset(TimeSpan.Zero).ToUniversalTime());
                }
                return r.Value as Base;
            });
        }
    }
}
