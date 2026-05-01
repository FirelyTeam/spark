/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.Collections.Generic;
using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Engine.Model;
using Spark.Engine.Search.Types;
using Expression = Spark.Engine.Search.Types.Expression;

namespace Spark.Engine.Search;

public class ElementIndexer : BaseElementIndexer
{
    public ElementIndexer(IFhirModel fhirModel, IReferenceNormalizationService referenceNormalizationService = null)
        : base(fhirModel, referenceNormalizationService)
    {
    }

    protected List<Expression> ToExpressions(Age element)
    {
        return element == null ? null : ToExpressions(element as Quantity);
    }

    protected List<Expression> ToExpressions(Location.PositionComponent element)
    {
        if (element?.Latitude == null)
            return null;
        if (element.Longitude == null)
            return null;

        var position = new List<IndexValue>
        {
            new("latitude", new NumberValue(element.Latitude.Value)),
            new("longitude", new NumberValue(element.Longitude.Value))
        };

        return ListOf(new CompositeValue(position));
    }

    protected List<Expression> ToExpressions(Timing element)
    {
        if (element?.Repeat?.Bounds == null)
            return null;

        // TODO: Should I handle Duration?
        return ToExpressions(element.Repeat.Bounds as Period);
    }
}
