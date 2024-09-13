/*
 * Copyright (c) 2020-2024, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;
using System;
using System.Linq;
using System.Reflection;

namespace Spark.Engine.Utility;

internal static class FhirPathUtil
{
    internal static string ConvertToXPathExpression(string fhirPathExpression)
    {
        const string prefix = "f:";
        const string separator = "/";

        string[] elements = fhirPathExpression.Split('.');
        string xPathExpression = string.Empty;
        foreach (var element in elements)
        {
            if (string.IsNullOrEmpty(xPathExpression))
                xPathExpression = $"{prefix}{element}";
            else
                xPathExpression += $"{separator}{prefix}{element}";
        }

        return xPathExpression;
    }

    internal static string ResolveToFhirPathExpression(Type resourceType, string expression)
    {
        Type rootType = resourceType;
        string[] elements = expression.Split('.');
        int length = elements.Length;
        string fhirPathExpression = string.Empty;
        Type currentType = rootType;
        for (int i = 0; length > i; i++)
        {
            (string, string) elementAndIndexer = GetElementSeparetedFromIndexer(elements[i]);
            (Type, string) resolvedElement = ResolveElement(currentType, elementAndIndexer.Item1);

            fhirPathExpression += $"{resolvedElement.Item2}{elementAndIndexer.Item2}.";

            currentType = resolvedElement.Item1;
        };

        return fhirPathExpression.Length == 0 ? fhirPathExpression : $"{rootType.Name}.{fhirPathExpression.TrimEnd('.')}";
    }

    internal static (Type, string) ResolveElement(Type root, string element)
    {
        PropertyInfo pi = root.GetProperty(element);
        if (pi == null) return (null, element);

        string fhirElementName = element;
        FhirElementAttribute fhirElement = pi.GetCustomAttribute<FhirElementAttribute>();
        if (fhirElement != null)
        {
            fhirElementName = fhirElement.Name;
        }

        Type elementType;
        if (pi.PropertyType.IsGenericType)
        {
            elementType = pi.PropertyType.GetGenericArguments().FirstOrDefault();
        }
        else
        {
            elementType = pi.PropertyType.UnderlyingSystemType;
        }

        return (elementType, fhirElementName);
    }

    internal static string GetFhirElementForResource<T>(string element)
        where T : Resource
    {
        MemberInfo mi = typeof(T).GetMember(element).FirstOrDefault();
        if (mi != null)
        {
            FhirElementAttribute fhirElement = mi.GetCustomAttribute<FhirElementAttribute>();
            if (fhirElement != null)
            {
                return fhirElement.Name;
            }
        }

        return element;
    }

    internal static (string, string) GetElementSeparetedFromIndexer(string element)
    {
        int index = element.LastIndexOf("[");
        if (index > -1)
        {
            return (element.Substring(0, index), element.Substring(index));
        }

        return (element, string.Empty);
    }
}