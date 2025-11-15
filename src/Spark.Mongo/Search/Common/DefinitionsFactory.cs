/* 
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2019-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using System.Collections.Generic;
using System.Linq;
using Spark.Engine.Core;

namespace Spark.Mongo.Search.Common;

public static class DefinitionsFactory
{
    private static Definition CreateDefinition(SearchParamDefinition paramdef)
    {
        return new Definition
        {
            Argument = ArgumentFactory.Create(paramdef.Type),
            Resource = paramdef.Resource,
            ParamName = paramdef.Name,
            Query = new ElementQuery(paramdef.Path),
            ParamType = paramdef.Type,
            Description = paramdef.Description?.Value
        };
    }

    public static Definitions Generate(IEnumerable<SearchParamDefinition> searchparameters)
    {
        var definitions = new Definitions();
        foreach (var searchParameter in searchparameters.Where(param => param.Path != null && param.Path.Length > 0))
        {
            var definition = CreateDefinition(searchParameter);
            definitions.Add(definition);
        }
        ManualCorrectDefinitions(definitions);
        return definitions;
    }

    private static void ManualCorrectDefinitions(Definitions items)
    {
        // These overrides are for those cases where the current meta-data does not help or is incorrect.
        items.Replace(new Definition() { Resource = "Patient", ParamName = "phonetic", Query = new ElementQuery("Patient.Name.Family", "Patient.Name.Given"), Argument = new FuzzyArgument() });
        items.Replace(new Definition() { Resource = "Practitioner", ParamName = "phonetic", Query = new ElementQuery("Practitioner.Name.Family", "Practitioner.Name.Given"), Argument = new FuzzyArgument() });
    }
}
