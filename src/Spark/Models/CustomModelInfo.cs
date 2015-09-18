using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static Hl7.Fhir.Model.ModelInfo;
using Hl7.Fhir.Model;

namespace Spark.Models
{
    public class CustomModelInfo

    {
        private static List<SearchParamDefinition> searchParameters;
        public static List<SearchParamDefinition> SearchParameters { get { return searchParameters; } }

        static CustomModelInfo()
        {
            searchParameters = new List<SearchParamDefinition>
            {
                new SearchParamDefinition() { Resource = "Practitioner", Name = "roleid", Description = @"Search by role identifier extension", Type = Conformance.SearchParamType.Token, Path = new string[] { @"Practitioner.practitionerRole.Extension[url=http://hl7.no/fhir/StructureDefinition/practitonerRole-identifier].ValueIdentifier" } }
            };
//            searchParameters.AddRange(ModelInfo.SearchParameters);
        }
    }
}