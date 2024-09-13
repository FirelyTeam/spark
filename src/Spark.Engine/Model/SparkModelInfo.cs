/* 
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * Copyright (c) 2019-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using Hl7.Fhir.Model;
using System.Collections.Generic;
using System.Linq;
using static Hl7.Fhir.Model.ModelInfo;

namespace Spark.Engine.Model;

public static class SparkModelInfo
{
    public static List<SearchParamDefinition> SparkSearchParameters = SearchParameters.Union(
        new List<SearchParamDefinition>()
        {
            new SearchParamDefinition() {Resource = "Composition", Name = "custodian", Description = new Markdown(@"custom search parameter on Composition for generating $document"), Type = SearchParamType.Reference, Path = new string[] {"Composition.custodian" }, XPath = "f:Composition/f:custodian", Expression = "Composition.custodian", Target =  new ResourceType[] { ResourceType.Organization}  }
            , new SearchParamDefinition() {Resource = "Composition", Name = "eventdetail", Description = new Markdown(@"custom search parameter on Composition for generating $document"), Type = SearchParamType.Reference, Path = new string[] {"Composition.event.detail" }, XPath = "f:Composition/f:event/f:detail", Expression = "Composition.event.detail", Target =  Enum.GetValues(typeof(ResourceType)).Cast<ResourceType>().ToArray()  }
            , new SearchParamDefinition() {Resource = "Encounter", Name = "serviceprovider", Description = new Markdown(@"Organization that provides the Encounter services."), Type = SearchParamType.Reference, Path = new[] {"Encounter.serviceProvider"}, XPath = "f:Encounter/f:serviceProvider",Expression = "Encounter.serviceProvider", Target =  new ResourceType[] { ResourceType.Organization} }
            , new SearchParamDefinition() {Resource = "Slot", Name = "provider", Description = new Markdown(@"Search Slot by provider extension"), Type = SearchParamType.Reference, Path = new string[] { @"Slot.extension[url=http://fhir.blackpear.com/era/Slot/provider].valueReference" }, Target =  new ResourceType[] { ResourceType.Organization}}
        }).ToList();
}
