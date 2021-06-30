/* 
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using Hl7.Fhir.Model;
using static Hl7.Fhir.Model.CapabilityStatement;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Engine.Core
{
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
            if (_endpoint != null && _endpoint.Count() > 0) messaging.Endpoint = _endpoint;
            if (_reliableCache != null) messaging.ReliableCacheElement = _reliableCache;
            if (_documentation != null) messaging.DocumentationElement = _documentation;
            if (_supportedMessage != null && _supportedMessage.Count() > 0) messaging.SupportedMessage = _supportedMessage;
            if (_event != null && _event.Count() > 0) messaging.Event = _event;
            
            return messaging;
        }

        public MessagingComponentBuilder WithEndpoint(Coding protocol, string address)
        {
            return WithEndpoint(protocol, !string.IsNullOrWhiteSpace(address) ? new FhirUri(address) : null);
        }
        
        public MessagingComponentBuilder WithEndpoint(Coding protocol, FhirUri address)
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
            WithDocumentation(!string.IsNullOrWhiteSpace(documentation) ? new FhirString(documentation) : null);
            return this;
        }
        
        public MessagingComponentBuilder WithDocumentation(FhirString documentation)
        {
            _documentation = documentation;
            return this;
        }

        public MessagingComponentBuilder WithSupportedMessage(EventCapabilityMode mode, string defintion = null)
        {
            return WithSupportedMessage(mode, !string.IsNullOrWhiteSpace(defintion) ? new ResourceReference(defintion) : null);
        }
        
        public MessagingComponentBuilder WithSupportedMessage(EventCapabilityMode mode, ResourceReference defintion = null)
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
            if (_event == null) _event = new List<EventComponent>();
            _event.Add(evt);
            return this;
        }
    }
}