/* 
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2019-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Engine.Core;
using Spark.Engine.Model;

namespace Spark.Store.MongoDB.Search.Common;

public static class DefinitionsFactory
{
    public static Definition CreateDefinition(IFhirModel fhirModel, SearchParameter searchParameter)
    {
        return new Definition
        {
            Argument = ArgumentFactory.Create(searchParameter.Type.GetValueOrDefault()),
            Resource = searchParameter.Resource,
            ParamName = searchParameter.Name,
            Query = new ElementQuery(fhirModel, searchParameter.Xpath),
            ParamType = searchParameter.Type.GetValueOrDefault(),
            Description = searchParameter.Description
        };
    }

    public static Definitions Generate(IFhirModel fhirModel)
    {
        var definitions = new Definitions();

        foreach (var searchParameter in fhirModel.SearchParameters)
        {
            if (!string.IsNullOrEmpty(searchParameter.Xpath))
            {
                Definition definition = CreateDefinition(fhirModel, searchParameter);
                definitions.Add(definition);
            }
        }
        ManualCorrectDefinitions(definitions, fhirModel);
        return definitions;
    }

    private static void ManualCorrectDefinitions(Definitions items, IFhirModel fhirModel)
    {
        // These overrides are for those cases where the current meta-data does not help or is incorrect.
        items.Replace(new Definition() { Resource = "Patient", ParamName = "phonetic", Query = new ElementQuery(fhirModel, "Patient.Name.Family", "Patient.Name.Given"), Argument = new FuzzyArgument() });
        items.Replace(new Definition() { Resource = "Practitioner", ParamName = "phonetic", Query = new ElementQuery(fhirModel, "Practitioner.Name.Family", "Practitioner.Name.Given"), Argument = new FuzzyArgument() });
    }
}
