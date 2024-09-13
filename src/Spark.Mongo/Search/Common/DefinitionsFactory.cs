/* 
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2019-2024, Incendi <info@incendi.no>
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
    public static Definition CreateDefinition(ModelInfo.SearchParamDefinition paramdef)
    {
        Definition definition = new Definition
        {
            Argument = ArgumentFactory.Create(paramdef.Type),
            Resource = paramdef.Resource,
            ParamName = paramdef.Name,
            Query = new ElementQuery(paramdef.Path),
            ParamType = paramdef.Type,
            Description = paramdef.Description?.Value
        };
        return definition;
    }

    public static Definitions Generate(IEnumerable<ModelInfo.SearchParamDefinition> searchparameters)
    {
        var definitions = new Definitions();

        foreach (var param in searchparameters)
        {
            if (param.Path != null && param.Path.Count() > 0)
            {
                Definition definition = CreateDefinition(param);
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