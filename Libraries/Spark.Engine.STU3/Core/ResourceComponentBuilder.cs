/* 
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using static Hl7.Fhir.Model.CapabilityStatement;

namespace Spark.Engine.Core;

public class ResourceComponentBuilder
{
    private Code<ResourceType> _type;
    private ResourceReference _profile;
    private List<ResourceInteractionComponent> _interaction;
    private Code<ResourceVersionPolicy> _versioning;
    private FhirBoolean _readHistory;
    private FhirBoolean _updateCreate;
    private FhirBoolean _conditionalCreate;
    private Code<ConditionalReadStatus> _conditionalRead;
    private FhirBoolean _conditionalUpdate;
    private Code<ConditionalDeleteStatus> _conditionalDelete;
    private List<Code<ReferenceHandlingPolicy>> _referencePolicy;
    private List<FhirString> _searchInclude;
    private List<FhirString> _searchRevInclude;
    private List<CapabilityStatement.SearchParamComponent> _searchParam;

    public ResourceComponent Build()
    {
        var resource = new ResourceComponent();
        if (_type == null) throw new RequiredAttributeException("Attribute 'Type' of ResourceComponent is required.");
        resource.TypeElement = _type;
        if (_profile != null) resource.Profile = _profile;
        if (_interaction is { Count: > 0 }) resource.Interaction = _interaction;
        if (_versioning != null) resource.VersioningElement = _versioning;
        if (_readHistory != null) resource.ReadHistoryElement = _readHistory;
        if (_updateCreate != null) resource.UpdateCreateElement = _updateCreate;
        if (_conditionalCreate != null) resource.ConditionalCreateElement = _conditionalCreate;
        if (_conditionalRead != null) resource.ConditionalReadElement = _conditionalRead;
        if (_conditionalUpdate != null) resource.ConditionalUpdateElement = _conditionalUpdate;
        if (_conditionalDelete != null) resource.ConditionalDeleteElement = _conditionalDelete;
        if (_referencePolicy is { Count: > 0 }) resource.ReferencePolicyElement = _referencePolicy;
        if (_searchInclude is { Count: > 0 }) resource.SearchIncludeElement = _searchInclude;
        if (_searchRevInclude is { Count: > 0 }) resource.SearchRevIncludeElement = _searchRevInclude;
        if (_searchParam is { Count: > 0 }) resource.SearchParam = _searchParam;

        return resource;
    }

    public ResourceComponentBuilder WithType(string type)
    {
        return WithType(new Code<ResourceType>(Enum.Parse<ResourceType>(type)));
    }
    
    public ResourceComponentBuilder WithType(Code<ResourceType> type)
    {
        _type = type;
        return this;
    }

    public ResourceComponentBuilder WithProfile(string profile)
    {
        return WithProfile(new ResourceReference(profile));
    }

    public ResourceComponentBuilder WithProfile(ResourceReference profile)
    {
        _profile = profile;
        return this;
    }

    public ResourceComponentBuilder WithInteraction(TypeRestfulInteraction code, string documentation = null)
    {
        return WithInteraction(new ResourceInteractionComponent {Code = code, Documentation = documentation});
    }

    public ResourceComponentBuilder WithInteraction(ResourceInteractionComponent interaction)
    {
        _interaction ??= [];
        if (interaction != null)
        {
            _interaction.Add(interaction);
        }
        return this;
    }

    public ResourceComponentBuilder WithVersioning(ResourceVersionPolicy versioning)
    {
        return WithVersioning(new Code<ResourceVersionPolicy>(versioning));
    }

    public ResourceComponentBuilder WithVersioning(Code<ResourceVersionPolicy> versioning)
    {
        _versioning = versioning;
        return this;
    }

    public ResourceComponentBuilder WithReadHistory(bool readHistory)
    {
        return WithReadHistory(new FhirBoolean(readHistory));
    }

    public ResourceComponentBuilder WithReadHistory(FhirBoolean readHistory)
    {
        _readHistory = readHistory;
        return this;
    }

    public ResourceComponentBuilder WithUpdateCreate(bool updateCreate)
    {
        return WithUpdateCreate(new FhirBoolean(updateCreate));
    }

    public ResourceComponentBuilder WithUpdateCreate(FhirBoolean updateCreate)
    {
        _updateCreate = updateCreate;
        return this;
    }

    public ResourceComponentBuilder WithConditionalCreate(bool conditionalCreate)
    {
        return WithConditionalCreate(new FhirBoolean(conditionalCreate));
    }

    public ResourceComponentBuilder WithConditionalCreate(FhirBoolean conditionalCreate)
    {
        _conditionalCreate = conditionalCreate;
        return this;
    }

    public ResourceComponentBuilder WithConditionalRead(ConditionalReadStatus? conditionalRead)
    {
        return WithConditionalRead(new Code<ConditionalReadStatus>(conditionalRead));
    }

    public ResourceComponentBuilder WithConditionalRead(Code<ConditionalReadStatus> conditionalRead)
    {
        _conditionalRead = conditionalRead;
        return this;
    }

    public ResourceComponentBuilder WithConditionalUpdate(bool? conditionalUpdate)
    {
        return WithConditionalUpdate(new FhirBoolean(conditionalUpdate));
    }

    public ResourceComponentBuilder WithConditionalUpdate(FhirBoolean conditionalUpdate)
    {
        _conditionalUpdate = conditionalUpdate;
        return this;
    }

    public ResourceComponentBuilder WithConditionalDelete(ConditionalDeleteStatus? conditionalDelete)
    {
        return WithConditionalDelete(new Code<ConditionalDeleteStatus>(conditionalDelete));
    }

    public ResourceComponentBuilder WithConditionalDelete(Code<ConditionalDeleteStatus> conditionalDelete)
    {
        _conditionalDelete = conditionalDelete;
        return this;
    }

    public ResourceComponentBuilder WithReferencePolicy(ReferenceHandlingPolicy referencePolicy)
    {
        return WithReferencePolicy(new Code<ReferenceHandlingPolicy>(referencePolicy));
    }

    public ResourceComponentBuilder WithReferencePolicy(Code<ReferenceHandlingPolicy> referencePolicy)
    {
        _referencePolicy ??= [];
        _referencePolicy.Add(referencePolicy);
        return this;
    }

    public ResourceComponentBuilder WithSearchInclude(string searchInclude)
    {
        return WithSearchInclude(new FhirString(searchInclude));
    }

    public ResourceComponentBuilder WithSearchInclude(FhirString searchInclude)
    {
        _searchInclude ??= [];
        _searchInclude.Add(searchInclude);
        return this;
    }

    public ResourceComponentBuilder WithSearchRevInclude(string searchRevInclude)
    {
        return WithSearchRevInclude(new FhirString(searchRevInclude));
    }

    public ResourceComponentBuilder WithSearchRevInclude(FhirString searchRevInclude)
    {
        _searchRevInclude ??= [];
        _searchRevInclude.Add(searchRevInclude);
        return this;
    }

    public ResourceComponentBuilder WithSearchParam(string name, SearchParamType type, string definition = null, string documentation = null)
    {
        return WithSearchParam(
            !string.IsNullOrWhiteSpace(name) ? new FhirString(name) : null,
            new Code<SearchParamType>(type),
            !string.IsNullOrWhiteSpace(definition) ? new FhirUri(definition) : null,
            !string.IsNullOrWhiteSpace(documentation) ? new FhirString(documentation) : null
        );
    }

    public ResourceComponentBuilder WithSearchParam(FhirString name, Code<SearchParamType> type, FhirUri definition = null, FhirString documentation = null)
    {
        return WithSearchParam(new CapabilityStatement.SearchParamComponent
        {
            NameElement = name,
            TypeElement = type,
            DefinitionElement = definition,
            DocumentationElement = documentation,
        });
    }

    public ResourceComponentBuilder WithSearchParam(CapabilityStatement.SearchParamComponent searchParam)
    {
        _searchParam ??= [];
        _searchParam.Add(searchParam);
        return this;
    }
}
