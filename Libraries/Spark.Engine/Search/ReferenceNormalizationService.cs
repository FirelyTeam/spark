/* 
 * Copyright (c) 2020-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Linq;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Search.Types;

namespace Spark.Engine.Search;

public class ReferenceNormalizationService : IReferenceNormalizationService
{
    private readonly ILocalhost _localhost;

    public ReferenceNormalizationService(ILocalhost localhost)
    {
        _localhost = localhost ?? throw new ArgumentNullException(nameof(localhost));
    }

    public ValueExpression GetNormalizedReferenceValue(ValueExpression originalValue, string resourceType)
    {
        if (originalValue == null)
        {
            return null;
        }
        var value = originalValue.ToString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return originalValue;
        }
        if (!value.Contains("/") && !string.IsNullOrWhiteSpace(resourceType))
        {
            return new StringValue($"{resourceType}/{value}");
        }
        if (Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var uri))
        {
            var key = KeyExtensions.ExtractKey(uri);
            if (_localhost.GetKeyKind(key) != KeyKind.Foreign) // Don't normalize external references (https://github.com/FirelyTeam/spark/issues/244).
            {
                var refUri = _localhost.RemoveBase(uri);
                return new StringValue(refUri.ToString().TrimStart('/'));
            }
        }
        return originalValue;
    }

    public Criterium GetNormalizedReferenceCriteria(Criterium criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        Expression operand;

        if (criteria.Operand is ChoiceValue choiceOperand)
        {
            var normalizedChoicesList = new ChoiceValue(
                choiceOperand.Choices.Select(choice =>
                        GetNormalizedReferenceValue(choice as UntypedValue, criteria.Modifier))
                    .Where(normalizedValue => normalizedValue != null)
                    .ToList());

            if (!normalizedChoicesList.Choices.Any())
            {
                return null; // Choice operator without choices: ignore it.
            }

            operand = normalizedChoicesList;
        }
        else
        {
            var normalizedValue = GetNormalizedReferenceValue(criteria.Operand as UntypedValue, criteria.Modifier);
            if (normalizedValue == null)
            {
                return null;
            }

            operand = normalizedValue;
        }

        var cloned = criteria.Clone();
        cloned.Modifier = null;
        cloned.Operand = operand;
        cloned.SearchParameters.AddRange(criteria.SearchParameters);
        return cloned;
    }
}
