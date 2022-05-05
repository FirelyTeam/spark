using System.Collections.Generic;
using static Hl7.Fhir.Model.ModelInfo;
using Hl7.Fhir.Model;

namespace Spark.Models
{
    public class CustomModelInfo

    {
        private static List<SearchParamDefinition> _searchParameters;
        public static List<SearchParamDefinition> SearchParameters { get { return _searchParameters; } }

        static CustomModelInfo()
        {
            _searchParameters = new List<SearchParamDefinition>
            {
                new SearchParamDefinition() { Resource = "Practitioner", Name = "roleid", Description = new Markdown(@"Search by role identifier extension"), Type = SearchParamType.Token, Path = new string[] { @"Practitioner.practitionerRole.Extension[url=http://hl7.no/fhir/StructureDefinition/practitonerRole-identifier].ValueIdentifier" } }
            };
        }
    }
}