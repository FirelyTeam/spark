/*
 * Copyright (c) 2023, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace Spark.Engine.Search
{
    public class ResourceResolver
    {
        private const string RESOURCE_TYPE_CAPTURE = "resourceType";
        private const string RESOURCE_ID_CAPTURE = "resourceId";

        private readonly Regex _referenceRegex;
        private readonly IStructureDefinitionSummaryProvider _structureDefinitionSummaryProvider;

        public ResourceResolver()
        {
            var resourceTypesPattern = string.Join("|", ModelInfo.SupportedResources);
            var referenceCaptureRegexPattern = $@"(?<{RESOURCE_TYPE_CAPTURE}>{resourceTypesPattern})\/(?<{RESOURCE_ID_CAPTURE}>[A-Za-z0-9\-\.]{{1,64}})(\/_history\/[A-Za-z0-9\-\.]{{1,64}})?";
            _referenceRegex = new Regex(referenceCaptureRegexPattern, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.ExplicitCapture);

            _structureDefinitionSummaryProvider = new PocoStructureDefinitionSummaryProvider();
        }

        public ITypedElement Resolve(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                return null;
            }

            var match = _referenceRegex.Match(reference);
            if (!match.Success)
            {
                return null;
            }

            string resourceTypeInString = match.Groups[RESOURCE_TYPE_CAPTURE].Value;
            string resourceId = match.Groups[RESOURCE_ID_CAPTURE].Value;
            ISourceNode node = FhirJsonNode.Create(
                JObject.FromObject(
                    new
                    {
                        resourceType = resourceTypeInString,
                        id = resourceId,
                    }));

            return node.ToTypedElement(_structureDefinitionSummaryProvider);
        }
    }
}