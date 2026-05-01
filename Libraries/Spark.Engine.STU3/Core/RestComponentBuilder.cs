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

public class RestComponentBuilder
{
    private Code<RestfulCapabilityMode> _mode;
    private FhirString _documentation;
    private SecurityComponent _security;
    private List<ResourceComponent> _resource;
    private List<SystemInteractionComponent> _interaction;
    private List<CapabilityStatement.SearchParamComponent> _searchParam;
    private List<OperationComponent> _operation;
    private List<FhirUri> _compartment;

    public RestComponent Build()
    {
        var rest = new RestComponent();
        if (_mode != null) rest.ModeElement = _mode;
        if (_documentation != null) rest.DocumentationElement = _documentation;
        if (_security != null) rest.Security = _security;
        if (_resource is { Count: > 0 }) rest.Resource = _resource;
        if (_interaction is { Count: > 0 }) rest.Interaction = _interaction;
        if (_searchParam is { Count: > 0 }) rest.SearchParam = _searchParam;
        if (_operation is { Count: > 0 }) rest.Operation = _operation;
        if (_compartment is { Count: > 0 }) rest.CompartmentElement = _compartment;

        return rest;
    }

    public RestComponentBuilder WithMode(RestfulCapabilityMode mode)
    {
        return WithMode(new Code<RestfulCapabilityMode>(mode));
    }

    public RestComponentBuilder WithMode(Code<RestfulCapabilityMode> mode)
    {
        _mode = mode;
        return this;
    }

    public RestComponentBuilder WithDocumentation(string documentation)
    {
        return WithDocumentation(!string.IsNullOrWhiteSpace(documentation) ? new FhirString(documentation) : null);
    }

    public RestComponentBuilder WithDocumentation(FhirString documentation)
    {
        _documentation = documentation;
        return this;
    }

    public RestComponentBuilder WithSecurity(bool cors, string description = null, List<CodeableConcept> service = null, List<CertificateComponent> certificate = null)
    {
        return WithSecurity(
            new FhirBoolean(cors),
            !string.IsNullOrEmpty(description) ? new FhirString(description) : null,
            service,
            certificate
        );
    }

    public RestComponentBuilder WithSecurity(FhirBoolean cors, FhirString description = null, List<CodeableConcept> service = null, List<CertificateComponent> certificate = null)
    {
        return WithSecurity(new SecurityComponent
        {
            CorsElement = cors,
            DescriptionElement = description,
            Service = service is { Count: > 0 } ? service : [],
            Certificate = certificate is { Count: > 0 } ? certificate : [],
        });
    }

    public RestComponentBuilder WithSecurity(SecurityComponent security)
    {
        _security = security;
        return this;
    }

    public RestComponentBuilder WithResource(Func<ResourceComponentBuilder, ResourceComponentBuilder> configure)
    {
        return WithResource(configure(new ResourceComponentBuilder()).Build());
    }

    public RestComponentBuilder WithResource(ResourceComponent resource)
    {
        _resource ??= [];
        if (resource != null)
        {
            _resource.Add(resource);
        }
        return this;
    }

    public RestComponentBuilder WithInteraction(SystemRestfulInteraction code, string documentation = null)
    {
        return WithInteraction(
            new Code<SystemRestfulInteraction>(code),
            !string.IsNullOrWhiteSpace(documentation) ? new FhirString(documentation) : null
        );
    }

    public RestComponentBuilder WithInteraction(Code<SystemRestfulInteraction> code, FhirString documentation = null)
    {
        return WithInteraction(new SystemInteractionComponent
        {
            CodeElement = code,
            DocumentationElement = documentation,
        });
    }

    public RestComponentBuilder WithInteraction(SystemInteractionComponent interaction)
    {
        _interaction ??= [];
        _interaction.Add(interaction);
        return this;
    }

    public RestComponentBuilder WithSearchParam(string name, SearchParamType type, string definition = null, string documentation = null)
    {
        return WithSearchParam(
            !string.IsNullOrWhiteSpace(name) ? new FhirString(name) : null,
            new Code<SearchParamType>(type),
            !string.IsNullOrWhiteSpace(definition) ? new FhirUri(definition) : null,
            !string.IsNullOrWhiteSpace(documentation) ? new FhirString(documentation) : null
        );
    }

    public RestComponentBuilder WithSearchParam(FhirString name, Code<SearchParamType> type, FhirUri definition = null, FhirString documentation = null)
    {
        _searchParam ??= [];
        var searchParam = new CapabilityStatement.SearchParamComponent
        {
            NameElement = name,
            TypeElement = type,
            DefinitionElement = definition,
            DocumentationElement = documentation,
        };
        _searchParam.Add(searchParam);
        return this;
    }

    public RestComponentBuilder WithSearchParam(CapabilityStatement.SearchParamComponent searchParam)
    {
        _searchParam ??= [];
        _searchParam.Add(searchParam);
        return this;
    }

    public RestComponentBuilder WithOperation(string name, string definition = null)
    {
        return WithOperation(
            !string.IsNullOrWhiteSpace(name) ? new FhirString(name) : null,
            !string.IsNullOrWhiteSpace(definition) ? new ResourceReference(definition) : null);
    }

    public RestComponentBuilder WithOperation(FhirString name, ResourceReference definition)
    {
        return WithOperation(new OperationComponent
        {
            NameElement = name,
            Definition = definition
        });
    }

    public RestComponentBuilder WithOperation(OperationComponent operation)
    {
        _operation ??= [];
        _operation.Add(operation);
        return this;
    }

    public RestComponentBuilder WithCompartment(string compartment)
    {
        return WithCompartment(new FhirUri(compartment));
    }

    public RestComponentBuilder WithCompartment(FhirUri compartment)
    {
        _compartment ??= [];
        _compartment.Add(compartment);
        return this;
    }
}
