/* 
 * Copyright (c) 2021-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;
using System.Collections.Generic;
using System.Linq;
using static Hl7.Fhir.Model.CapabilityStatement;

namespace Spark.Engine.Core;

public class ResourceComponentBuilder
{
    private Code _type;
    private Canonical _profile;
    private List<Canonical> _supportedProfile;
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
    private List<SearchParamComponent> _searchParam;
    private List<OperationComponent> _operation;

    public ResourceComponent Build()
    {
        var resource = new ResourceComponent();
        if (_type == null) throw new RequiredAttributeException("Attribute 'Type' of ResourceComponent is required.");
        resource.TypeElement = _type;
        if (_profile != null) resource.ProfileElement = _profile;
        if (_supportedProfile != null && _supportedProfile.Count > 0) resource.SupportedProfileElement = _supportedProfile;
        if (_interaction != null && _interaction.Count() > 0) resource.Interaction = _interaction;
        if (_versioning != null) resource.VersioningElement = _versioning;
        if (_readHistory != null) resource.ReadHistoryElement = _readHistory;
        if (_updateCreate != null) resource.UpdateCreateElement = _updateCreate;
        if (_conditionalCreate != null) resource.ConditionalCreateElement = _conditionalCreate;
        if (_conditionalRead != null) resource.ConditionalReadElement = _conditionalRead;
        if (_conditionalUpdate != null) resource.ConditionalUpdateElement = _conditionalUpdate;
        if (_conditionalDelete != null) resource.ConditionalDeleteElement = _conditionalDelete;
        if (_referencePolicy != null && _referencePolicy.Count() > 0) resource.ReferencePolicyElement = _referencePolicy;
        if (_searchParam != null && _searchParam.Count() > 0) resource.SearchParam = _searchParam;
        if (_operation != null && _operation.Count > 0) resource.Operation = _operation;

        return resource;
    }

    public ResourceComponentBuilder WithType(ResourceType type)
    {
        return WithType(new Code(type.GetLiteral()));
    }

    public ResourceComponentBuilder WithType(Code type)
    {
        _type = type;
        return this;
    }

    public ResourceComponentBuilder WithProfile(string profile)
    {
        return WithProfile(new Canonical(profile));
    }

    public ResourceComponentBuilder WithProfile(Canonical profile)
    {
        _profile = profile;
        return this;
    }

    public ResourceComponentBuilder WithSupportedProfile(string supportedProfile)
    {
        return WithProfile(string.IsNullOrWhiteSpace(supportedProfile) ? null : new Canonical(supportedProfile));
    }

    public ResourceComponentBuilder WithSupportedProfile(Canonical supportedProfile)
    {
        if (_supportedProfile == null) _supportedProfile = new List<Canonical>();
        _supportedProfile.Add(supportedProfile);
        return this;
    }

    public ResourceComponentBuilder WithInteraction(TypeRestfulInteraction code, Markdown documentation = null)
    {
        return WithInteraction(new ResourceInteractionComponent {Code = code, Documentation = documentation});
    }

    public ResourceComponentBuilder WithInteraction(ResourceInteractionComponent interaction)
    {
        if (_interaction == null) _interaction = new List<ResourceInteractionComponent>();
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
        if (_referencePolicy == null) _referencePolicy = new List<Code<ReferenceHandlingPolicy>>();
        _referencePolicy.Add(referencePolicy);
        return this;
    }

    public ResourceComponentBuilder WithSearchInclude(string searchInclude)
    {
        return WithSearchInclude(new FhirString(searchInclude));
    }

    public ResourceComponentBuilder WithSearchInclude(FhirString searchInclude)
    {
        if (_searchInclude == null) _searchInclude = new List<FhirString>();
        _searchInclude.Add(searchInclude);
        return this;
    }

    public ResourceComponentBuilder WithSearchRevInclude(string searchRevInclude)
    {
        return WithSearchRevInclude(new FhirString(searchRevInclude));
    }

    public ResourceComponentBuilder WithSearchRevInclude(FhirString searchRevInclude)
    {
        if (_searchRevInclude == null) _searchRevInclude = new List<FhirString>();
        _searchRevInclude.Add(searchRevInclude);
        return this;
    }

    public ResourceComponentBuilder WithSearchParam(string name, SearchParamType type, string defintion = null, string documentation = null)
    {
        return WithSearchParam(
            !string.IsNullOrWhiteSpace(name) ? new FhirString(name) : null,
            new Code<SearchParamType>(type),
            !string.IsNullOrWhiteSpace(defintion) ? new Canonical(defintion) : null,
            !string.IsNullOrWhiteSpace(documentation) ? new Markdown(documentation) : null
        );
    }

    public ResourceComponentBuilder WithSearchParam(FhirString name, Code<SearchParamType> type, Canonical defintion = null, Markdown documentation = null)
    {
        return WithSearchParam(new SearchParamComponent
        {
            NameElement = name,
            TypeElement = type,
            DefinitionElement = defintion,
            Documentation = documentation,
        });
    }

    public ResourceComponentBuilder WithSearchParam(SearchParamComponent searchParam)
    {
        if (_searchParam == null) _searchParam = new List<SearchParamComponent>();
        _searchParam.Add(searchParam);
        return this;
    }

    public ResourceComponentBuilder WithOperation(string name, string definition, string documentation = null)
    {
        return WithOperation(
            string.IsNullOrWhiteSpace(name) ? null : new FhirString(name),
            string.IsNullOrWhiteSpace(definition) ? null : new Canonical(definition),
            string.IsNullOrWhiteSpace(documentation) ? null : new Markdown(documentation)
        );
    }

    public ResourceComponentBuilder WithOperation(FhirString name, Canonical definition, Markdown documentation)
    {
        return WithOperation(new OperationComponent
        {
            NameElement = name,
            DefinitionElement = definition,
            Documentation = documentation,
        });
    }

    public ResourceComponentBuilder WithOperation(OperationComponent operation)
    {
        if (_operation == null) _operation = new List<OperationComponent>();
        _operation.Add(operation);
        return this;
    }
}
