using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Serialization;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Spark.Engine.Core
{
    public class LightweightReferenceToElementResolver : IReferenceToElementResolver
    {
        private readonly IFhirModel _fhirModel;
        private readonly string _resourceTypeCapture = "resourceType";
        private readonly string _resourceIdCapture = "resourceId";
        private readonly Regex _referenceRegex = null;

        public LightweightReferenceToElementResolver(IFhirModel fhirModel)
        {
            _fhirModel = fhirModel;

            var resourceTypesPattern = string.Join("|", _fhirModel.SupportedResources);
            var referenceCaptureRegexPattern = $@"(?<{_resourceTypeCapture}>{resourceTypesPattern})\/(?<{_resourceIdCapture}>[A-Za-z0-9\-\.]{{1,64}})(\/_history\/[A-Za-z0-9\-\.]{{1,64}})?";
            _referenceRegex = new Regex(
            referenceCaptureRegexPattern,
            RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.ExplicitCapture);
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

            string resourceTypeInString = match.Groups[_resourceTypeCapture].Value;
            string resourceId = match.Groups[_resourceIdCapture].Value;
            ISourceNode node = FhirJsonNode.Create(
                JObject.FromObject(
                    new
                    {
                        resourceType = resourceTypeInString,
                        id = resourceId,
                    }));

            return node.ToTypedElement(_fhirModel.StructureDefinitionSummaryProvider);
        }
    }
}
