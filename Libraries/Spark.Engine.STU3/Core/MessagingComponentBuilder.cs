/* 
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using static Hl7.Fhir.Model.CapabilityStatement;
using System.Collections.Generic;

namespace Spark.Engine.Core;

public class MessagingComponentBuilder
{
    private List<EndpointComponent> _endpoint;
    private UnsignedInt _reliableCache;
    private FhirString _documentation;
    private List<SupportedMessageComponent> _supportedMessage;
    private List<EventComponent> _event;

    public MessagingComponent Build()
    {
        var messaging = new MessagingComponent();
        if (_endpoint is { Count: > 0 }) messaging.Endpoint = _endpoint;
        if (_reliableCache != null) messaging.ReliableCacheElement = _reliableCache;
        if (_documentation != null) messaging.DocumentationElement = _documentation;
        if (_supportedMessage is { Count: > 0 }) messaging.SupportedMessage = _supportedMessage;
        if (_event is { Count: > 0 }) messaging.Event = _event;

        return messaging;
    }

    public MessagingComponentBuilder WithEndpoint(Coding protocol, string address)
    {
        return WithEndpoint(protocol, !string.IsNullOrWhiteSpace(address) ? new FhirUri(address) : null);
    }

    public MessagingComponentBuilder WithEndpoint(Coding protocol, FhirUri address)
    {
        _endpoint ??= [];
        var endpoint = new EndpointComponent
        {
            Protocol = protocol,
            AddressElement = address,
        };
        return WithEndpoint(endpoint);
    }

    public MessagingComponentBuilder WithEndpoint(EndpointComponent endpoint)
    {
        _endpoint ??= [];
        if (endpoint != null) _endpoint.Add(endpoint);
        return this;
    }

    public MessagingComponentBuilder WithReliableCache(int? reliableCache)
    {
        return WithReliableCache(new UnsignedInt(reliableCache));
    }

    public MessagingComponentBuilder WithReliableCache(UnsignedInt reliableCache)
    {
        _reliableCache = reliableCache;
        return this;
    }

    public MessagingComponentBuilder WithDocumentation(string documentation)
    {
        WithDocumentation(!string.IsNullOrWhiteSpace(documentation) ? new FhirString(documentation) : null);
        return this;
    }

    public MessagingComponentBuilder WithDocumentation(FhirString documentation)
    {
        _documentation = documentation;
        return this;
    }

    public MessagingComponentBuilder WithSupportedMessage(EventCapabilityMode mode, string definition = null)
    {
        return WithSupportedMessage(mode, !string.IsNullOrWhiteSpace(definition) ? new ResourceReference(definition) : null);
    }

    public MessagingComponentBuilder WithSupportedMessage(EventCapabilityMode mode, ResourceReference definition)
    {
        _supportedMessage ??= [];
        var supportedMessage = new SupportedMessageComponent
        {
            Mode = mode,
            Definition = definition,
        };
        return WithSupportedMessage(supportedMessage);
    }
    public MessagingComponentBuilder WithSupportedMessage(SupportedMessageComponent supportedMessage)
    {
        _supportedMessage ??= [];
        if (supportedMessage != null) _supportedMessage.Add(supportedMessage);
        return this;
    }

    public MessagingComponentBuilder WithEvent(Coding code, EventCapabilityMode mode, ResourceType focus, string request, string response, MessageSignificanceCategory? category = null, string documentation = null)
    {

        return WithEvent(code, mode, focus, new ResourceReference(request), new ResourceReference(response), category, documentation);
    }

    public MessagingComponentBuilder WithEvent(Coding code, EventCapabilityMode mode, ResourceType focus, ResourceReference request, ResourceReference response, MessageSignificanceCategory? category = null, string documentation = null)
    {
        var evt = new EventComponent
        {
            Code = code,
            Mode = mode,
            Focus = focus,
            Request = request,
            Response = response,
            Category = category,
            Documentation = !string.IsNullOrWhiteSpace(documentation) ? documentation : null,
        };
        return WithEvent(evt);
    }

    public MessagingComponentBuilder WithEvent(EventComponent evt)
    {
        _event ??= [];
        _event.Add(evt);
        return this;
    }
}
