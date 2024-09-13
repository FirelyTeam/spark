/* 
 * Copyright (c) 2021-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using static Hl7.Fhir.Model.CapabilityStatement;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Engine.Core;

public class MessagingComponentBuilder
{
    private List<EndpointComponent> _endpoint;
    private UnsignedInt _reliableCache;
    private Markdown _documentation;
    private List<SupportedMessageComponent> _supportedMessage;

    public MessagingComponent Build()
    {
        var messaging = new MessagingComponent();
        if (_endpoint != null && _endpoint.Count() > 0) messaging.Endpoint = _endpoint;
        if (_reliableCache != null) messaging.ReliableCacheElement = _reliableCache;
        if (_documentation != null) messaging.Documentation = _documentation;
        if (_supportedMessage != null && _supportedMessage.Count() > 0) messaging.SupportedMessage = _supportedMessage;

        return messaging;
    }

    public MessagingComponentBuilder WithEndpoint(Coding protocol, string address)
    {
        return WithEndpoint(protocol, !string.IsNullOrWhiteSpace(address) ? new FhirUrl(address) : null);
    }

    public MessagingComponentBuilder WithEndpoint(Coding protocol, FhirUrl address)
    {
        if (_endpoint == null) _endpoint = new List<EndpointComponent>();
        var endpoint = new EndpointComponent
        {
            Protocol = protocol,
            AddressElement = address,
        };
        return WithEndpoint(endpoint);
    }

    public MessagingComponentBuilder WithEndpoint(EndpointComponent endpoint)
    {
        if (_endpoint == null) _endpoint = new List<EndpointComponent>();
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
        WithDocumentation(!string.IsNullOrWhiteSpace(documentation) ? new Markdown(documentation) : null);
        return this;
    }

    public MessagingComponentBuilder WithDocumentation(Markdown documentation)
    {
        _documentation = documentation;
        return this;
    }

    public MessagingComponentBuilder WithSupportedMessage(EventCapabilityMode mode, string defintion = null)
    {
        return WithSupportedMessage(mode, !string.IsNullOrWhiteSpace(defintion) ? new Canonical(defintion) : null);
    }

    public MessagingComponentBuilder WithSupportedMessage(EventCapabilityMode mode, Canonical defintion = null)
    {
        if (_supportedMessage == null) _supportedMessage = new List<SupportedMessageComponent>();
        var supportedMessage = new SupportedMessageComponent
        {
            Mode = mode,
            Definition = defintion,
        };
        return WithSupportedMessage(supportedMessage);
    }
    public MessagingComponentBuilder WithSupportedMessage(SupportedMessageComponent supportedMessage)
    {
        if (_supportedMessage == null) _supportedMessage = new List<SupportedMessageComponent>();
        if (supportedMessage != null) _supportedMessage.Add(supportedMessage);
        return this;
    }
}
