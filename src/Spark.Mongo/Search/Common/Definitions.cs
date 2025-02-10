/* 
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.Collections.Generic;
using Hl7.Fhir.Model;
using Spark.Engine.Core;

namespace Spark.Mongo.Search.Common;
/*
Ik heb deze class losgetrokken van SearchParamDefinition,
omdat Definition onafhankelijk van Spark zou moeten kunnen bestaan.
Er komt dus een converter voor in de plaats. -mh
*/

public class Definition
{
    public Argument Argument { get; set; }
    public string Resource { get; set; }
    public string ParamName { get; set; }
    public string Description { get; set; }
    public SearchParamType ParamType { get; set; }
    public ElementQuery Query { get; set; }

    public override string ToString()
    {
        _ = Query.ToString();
        return string.Format("{0}.{1}->{2}", Resource.ToLower(), ParamName.ToLower(), Query.ToString());
    }
}

public class Definitions
{
    private List<Definition> _definitions = new List<Definition>();

    public void Add(Definition definition)
    {
        _definitions.Add(definition);
    }
    public void Replace(Definition definition)
    {
        _definitions.RemoveAll(d => (d.Resource == definition.Resource) && (d.ParamName == definition.ParamName));
        _definitions.Add(definition);
    }
}
