using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Hl7.Fhir.Model.ModelInfo;

namespace Spark.Engine.Model
{
    public static class SparkModelInfo
    {
        public static Assembly ApiAssembly()
        {
            return Assembly.GetAssembly(typeof(Resource));
        }

        public static List<SearchParamDefinition> SparkSearchParameters = ModelInfo.SearchParameters.Union(
            new List<SearchParamDefinition>()
        {
            new SearchParamDefinition() {Resource = "Composition", Name = "custodian", Description = @"custom search parameter on Composition for generating $document", Type = SearchParamType.Reference, Path = new string[] {"Composition.custodian" }, XPath = "f:Composition/f:custodian", Expression = "Composition.custodian" }
            , new SearchParamDefinition() {Resource = "Composition", Name = "eventdetail", Description = @"custom search parameter on Composition for generating $document", Type = SearchParamType.Reference, Path = new string[] {"Composition.event.detail" }, XPath = "f:Composition/f:event/f:detail", Expression = "Composition.event.detail" }
            , new SearchParamDefinition() {Resource = "Encounter", Name = "serviceprovider", Description = @"Organization that provides the Encounter services.", Type = SearchParamType.Reference, Path = new[] {"Encounter.serviceProvider"}, XPath = "f:Encounter/f:serviceProvider",Expression = "Encounter.serviceProvider" }
        }).ToList();
    }
}
