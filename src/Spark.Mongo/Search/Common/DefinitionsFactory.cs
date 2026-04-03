/* 
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2019-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.Collections.Generic;
using Spark.Engine.Core;
using Spark.Engine.Model;

namespace Spark.Mongo.Search.Common;

public static class DefinitionsFactory
{
    public static Definition CreateDefinition(SearchParameter searchParameter)
    {
        Definition definition = new Definition
        {
            Argument = ArgumentFactory.Create(searchParameter.Type.GetValueOrDefault()),
            Resource = searchParameter.Resource,
            ParamName = searchParameter.Name,
            Query = new ElementQuery(searchParameter.Xpath),
            ParamType = searchParameter.Type.GetValueOrDefault(),
            Description = searchParameter.Description
        };
        return definition;
    }

    public static Definitions Generate(IEnumerable<SearchParameter> searchParameters)
    {
        var definitions = new Definitions();

        foreach (var searchParameter in searchParameters)
        {
            if (!string.IsNullOrEmpty(searchParameter.Xpath))
            {
                Definition definition = CreateDefinition(searchParameter);
                definitions.Add(definition);
            }
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
