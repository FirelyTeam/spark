﻿/* 
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using static Hl7.Fhir.Model.CapabilityStatement;

namespace Spark.Engine.Core
{
    public class RestComponentBuilder
    {
        private Code<RestfulCapabilityMode> _mode;
        private FhirString _documentation;
        private SecurityComponent _security;
        private List<ResourceComponent> _resource;
        private List<SystemInteractionComponent> _interaction;
        private List<SearchParamComponent> _searchParam;
        private List<OperationComponent> _operation;
        private List<FhirUri> _compartment;
        
        public RestComponent Build()
        {
            var rest = new RestComponent();
            if (_mode != null) rest.ModeElement = _mode;
            if (_documentation != null) rest.DocumentationElement = _documentation;
            if (_security != null) rest.Security = _security;
            if (_resource != null && _resource.Count() > 0) rest.Resource = _resource;
            if (_interaction != null && _interaction.Count() > 0) rest.Interaction = _interaction;
            if (_searchParam != null && _searchParam.Count() > 0) rest.SearchParam = _searchParam;
            if (_operation != null && _operation.Count() > 0) rest.Operation = _operation;
            if (_compartment != null && _compartment.Count() > 0) rest.CompartmentElement = _compartment;
            
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
                Service = service != null && service.Count > 0 ? service : null,
                Certificate = certificate != null && certificate.Count > 0 ? certificate : null,
            });
        }
        
        public RestComponentBuilder WithSecurity(SecurityComponent security)
        {
            _security = security;
            return this;
        }

        public RestComponentBuilder WithResource(Func<ResourceComponent> configure)
        { 
            return WithResource(configure());
        }
        
        public RestComponentBuilder WithResource(ResourceComponent resource)
        { 
            if (_resource == null) _resource = new List<ResourceComponent>();
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
            if (_interaction == null) _interaction = new List<SystemInteractionComponent>();
            _interaction.Add(interaction);
            return this;
        }
        
        public RestComponentBuilder WithSearchParam(string name, SearchParamType type, string defintion = null, string documentation = null)
        {
            return WithSearchParam(
                !string.IsNullOrWhiteSpace(name) ? new FhirString(name) : null,
                new Code<SearchParamType>(type),
                !string.IsNullOrWhiteSpace(defintion) ? new FhirUri(defintion) : null,
                !string.IsNullOrWhiteSpace(documentation) ? new FhirString(documentation) : null
                );
        }
        
        public RestComponentBuilder WithSearchParam(FhirString name, Code<SearchParamType> type, FhirUri defintion = null, FhirString documentation = null)
        {
            if (_searchParam == null) _searchParam = new List<SearchParamComponent>();
            var searchParam = new SearchParamComponent
            {
                NameElement = name,
                TypeElement = type,
                DefinitionElement = defintion,
                DocumentationElement = documentation,
            };
            _searchParam.Add(searchParam);
            return this;
        }
        
        public RestComponentBuilder WithSearchParam(SearchParamComponent searchParam)
        {
            if (_searchParam == null) _searchParam = new List<SearchParamComponent>();
            _searchParam.Add(searchParam);
            return this;
        }
        
        public RestComponentBuilder WithOperation(string name, string defintion = null)
        {
            return WithOperation(
                !string.IsNullOrWhiteSpace(name) ? new FhirString(name) : null, 
                !string.IsNullOrWhiteSpace(defintion) ? new ResourceReference(defintion) : null);
        }
        
        public RestComponentBuilder WithOperation(FhirString name, ResourceReference defintion = null)
        {
            return WithOperation(new OperationComponent
            {
                NameElement = name,
                Definition = defintion
            });
        }
        
        public RestComponentBuilder WithOperation(OperationComponent operation)
        {
            if (_operation == null) _operation = new List<OperationComponent>();
            _operation.Add(operation);
            return this;
        }
        
        public RestComponentBuilder WithCompartment(string compartment)
        {
            return WithCompartment(new FhirUri(compartment));
        }
        
        public RestComponentBuilder WithCompartment(FhirUri compartment)
        {
            if (_compartment == null) _compartment = new List<FhirUri>();
            _compartment.Add(compartment);
            return this;
        }
    }
}
