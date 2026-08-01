/* 
 * Copyright (c) 2023-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Spark.Engine.Search;

public class ResourceResolver
{
    private const string ResourceTypeCapture = "resourceType";
    private const string ResourceIdCapture = "resourceId";

    private readonly Regex _referenceRegex;
    private readonly IStructureDefinitionSummaryProvider _structureDefinitionSummaryProvider;

    public ResourceResolver(IReadOnlyList<string> supportedResources, IStructureDefinitionSummaryProvider structureDefinitionSummaryProvider)
    {
        var resourceTypesPattern = string.Join("|", supportedResources);
        var referenceCaptureRegexPattern = $@"(?<{ResourceTypeCapture}>{resourceTypesPattern})\/(?<{ResourceIdCapture}>[A-Za-z0-9\-\.]{{1,64}})(\/_history\/[A-Za-z0-9\-\.]{{1,64}})?";
        _referenceRegex = new Regex(referenceCaptureRegexPattern, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        _structureDefinitionSummaryProvider = structureDefinitionSummaryProvider;
    }

    public PocoNode Resolve(string reference)
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

        string resourceTypeInString = match.Groups[ResourceTypeCapture].Value;
        string resourceId = match.Groups[ResourceIdCapture].Value;
        ISourceNode node = FhirJsonNode.Create(
            JObject.FromObject(
                new
                {
                    resourceType = resourceTypeInString,
                    id = resourceId,
                }));

        return node
            .ToTypedElement(_structureDefinitionSummaryProvider)
            .ToPocoNode();
    }
}
