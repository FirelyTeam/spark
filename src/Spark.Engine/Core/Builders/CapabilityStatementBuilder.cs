/* 
 * Copyright (c) 2021-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using static Hl7.Fhir.Model.CapabilityStatement;

namespace Spark.Engine.Core;

public class CapabilityStatementBuilder
{
    private FhirUri _url;
    private FhirString _version;
    private FhirString _name;
    private FhirString _title;
    private Code<PublicationStatus> _status;
    private FhirBoolean _experimental;
    private FhirDateTime _date;
    private FhirString _publisher;
    private List<ContactDetail> _contact;
    private Markdown _description;
    private List<UsageContext> _useContext;
    private List<CodeableConcept> _jurisdiction;
    private Markdown _purpose;
    private Markdown _copyright;
    private Code<CapabilityStatementKind> _kind;
    private List<Canonical> _instantiates;
    private List<Canonical> _imports;
    private SoftwareComponent _software;
    private ImplementationComponent _implementation;
    private Code<FHIRVersion> _fhirVersion;
    private List<Code> _format;
    private List<Code> _patchFormat;
    private List<Canonical> _implementationGuide;
    private List<ResourceReference> _profile;
    private List<RestComponent> _rest;
    private List<MessagingComponent> _messaging;
    private List<DocumentComponent> _document;

    public CapabilityStatement Build()
    {
        var capabilityStatement = new CapabilityStatement();
        if (_url != null) capabilityStatement.UrlElement = _url;
        if (_version != null) capabilityStatement.VersionElement = _version;
        if (_name != null) capabilityStatement.NameElement = _name;
        if (_title != null) capabilityStatement.TitleElement = _title;
        if (_status != null) capabilityStatement.StatusElement = _status;
        if (_experimental != null) capabilityStatement.ExperimentalElement = _experimental;
        if (_date != null) capabilityStatement.DateElement = _date;
        if (_publisher == null) capabilityStatement.PublisherElement = _publisher;
        if (_contact != null && _contact.Count > 0) capabilityStatement.Contact = _contact;
        if (_description != null) capabilityStatement.Description = _description;
        if (_useContext != null && _useContext.Count > 0) capabilityStatement.UseContext = _useContext;
        if (_jurisdiction != null && _jurisdiction.Count > 0) capabilityStatement.Jurisdiction = _jurisdiction;
        if (_purpose != null) capabilityStatement.Purpose = _purpose;
        if (_copyright != null) capabilityStatement.Copyright = _copyright;
        if (_kind != null) capabilityStatement.KindElement = _kind;
        if (_instantiates != null && _instantiates.Count > 0) capabilityStatement.InstantiatesElement = _instantiates;
        if (_imports != null && _imports.Count > 0) capabilityStatement.ImportsElement = _imports;
        if (_software != null) capabilityStatement.Software = _software;
        if (_implementation != null) capabilityStatement.Implementation = _implementation;
        if (_fhirVersion != null) capabilityStatement.FhirVersionElement = _fhirVersion;
        if (_format != null && _format.Count > 0) capabilityStatement.FormatElement = _format;
        if (_patchFormat != null && _patchFormat.Count > 0) capabilityStatement.PatchFormatElement = _patchFormat;
        if (_implementationGuide != null && _implementationGuide.Count > 0) capabilityStatement.ImplementationGuideElement = _implementationGuide;
        if (_rest != null && _rest.Count > 0) capabilityStatement.Rest = _rest;
        if (_messaging != null && _messaging.Count > 0) capabilityStatement.Messaging = _messaging;
        if (_document != null) capabilityStatement.Document = _document;
        return capabilityStatement;
    }

    public CapabilityStatementBuilder WithUrl(string url)
    {
        return WithUrl(!string.IsNullOrWhiteSpace(url) ? new FhirUri(url) : null);
    }
        
    public CapabilityStatementBuilder WithUrl(FhirUri url)
    {
        _url = url;
        return this;
    }

    public CapabilityStatementBuilder WithVersion(string version)
    {
        return WithVersion(!string.IsNullOrWhiteSpace(version) ? new FhirString(version) : null);
    }
        
    public CapabilityStatementBuilder WithVersion(FhirString version)
    {
        _version = version;
        return this;
    }

    public CapabilityStatementBuilder WithName(string name)
    {
        return WithName(!string.IsNullOrWhiteSpace(name) ? new FhirString(name) : null);
    }
        
    public CapabilityStatementBuilder WithName(FhirString name)
    {
        _name = name;
        return this;
    }

    public CapabilityStatementBuilder WithTitle(string title)
    {
        return WithTitle(!string.IsNullOrWhiteSpace(title) ? new FhirString(title) : null);
    }
        
    public CapabilityStatementBuilder WithTitle(FhirString title)
    {
        _title = title;
        return this;
    }

    public CapabilityStatementBuilder WithStatus(PublicationStatus status)
    {
        return WithStatus(new Code<PublicationStatus>(status));
    }
        
    public CapabilityStatementBuilder WithStatus(Code<PublicationStatus> status)
    {
        _status = status;
        return this;
    }

    public CapabilityStatementBuilder WithExperimental(bool experimental)
    {
        return WithExperimental(new FhirBoolean(experimental));
    }
        
    public CapabilityStatementBuilder WithExperimental(FhirBoolean experimental)
    {
        _experimental = experimental;
        return this;
    }

    public CapabilityStatementBuilder WithDate(FhirDateTime date)
    {
        _date = date;
        return this;
    }

    public CapabilityStatementBuilder WithDate(DateTimeOffset date)
    {
        return WithDate(new FhirDateTime(date));
    }

    public CapabilityStatementBuilder WithPublisher(string publisher)
    {
        return WithPublisher(!string.IsNullOrWhiteSpace(publisher) ? new FhirString(publisher) : null);
    }
        
    public CapabilityStatementBuilder WithPublisher(FhirString publisher)
    {
        _publisher = publisher;
        return this;
    }

    public CapabilityStatementBuilder WithContact(ContactDetail contact)
    {
        if (_contact == null) _contact = new List<ContactDetail>();
        _contact.Add(contact);
        return this;
    }

    public CapabilityStatementBuilder WithDescription(string description)
    {
        WithDescription(new Markdown(description));
        return this;
    }

    public CapabilityStatementBuilder WithDescription(Markdown description)
    {
        _description = description;
        return this;
    }

    public CapabilityStatementBuilder WithUseContext(UsageContext useContext)
    {
        if (_useContext == null) _useContext = new List<UsageContext>();
        _useContext.Add(useContext);
        return this;
    }

    public CapabilityStatementBuilder WithJurisdiction(CodeableConcept jurisdiction)
    {
        if (_jurisdiction == null) _jurisdiction = new List<CodeableConcept>();
        _jurisdiction.Add(jurisdiction);
        return this;
    }

    public CapabilityStatementBuilder WithPurpose(string purpose)
    {
        WithPurpose(new Markdown(purpose));
        return this;
    }

    public CapabilityStatementBuilder WithPurpose(Markdown purpose)
    {
        _purpose = purpose;
        return this;
    }

    public CapabilityStatementBuilder WithCopyright(string copyright)
    {
        WithCopyright(new Markdown(copyright));
        return this;
    }

    public CapabilityStatementBuilder WithCopyright(Markdown copyright)
    {
        _copyright = copyright;
        return this;
    }

    public CapabilityStatementBuilder WithKind(CapabilityStatementKind kind)
    {
        return WithKind(new Code<CapabilityStatementKind>(kind));
    }
        
    public CapabilityStatementBuilder WithKind(Code<CapabilityStatementKind> kind)
    {
        _kind = kind;
        return this;
    }

    public CapabilityStatementBuilder WithInstantiates(string instantiates)
    {
        return WithInstantiates(new Canonical(instantiates));
    }

    public CapabilityStatementBuilder WithInstantiates(Canonical instantiates)
    {
        if (_instantiates == null) _instantiates = new List<Canonical>();
        _instantiates.Add(instantiates);
        return this;
    }

    public CapabilityStatementBuilder WithImports(string imports)
    {
        return WithImports(new Canonical(imports));
    }

    public CapabilityStatementBuilder WithImports(Canonical imports)
    {
        if (_imports == null) _imports = new List<Canonical>();
        _imports.Add(imports);
        return this;
    }

    public CapabilityStatementBuilder WithSoftware(string name = null, string version = null, DateTimeOffset? releaseDate = null)
    {
        return WithSoftware(
            !string.IsNullOrWhiteSpace(name) ? new FhirString(name) : null, 
            !string.IsNullOrWhiteSpace(version) ? new FhirString(version) : null,
            releaseDate.HasValue ? new FhirDateTime(releaseDate.Value) : null
        );
    }
        
    public CapabilityStatementBuilder WithSoftware(FhirString name = null, FhirString version = null, FhirDateTime releaseDate = null)
    {
        return WithSoftware(new SoftwareComponent
        {
            NameElement = name,
            VersionElement = version,
            ReleaseDateElement = releaseDate,
        });
    }
        
    public CapabilityStatementBuilder WithSoftware(SoftwareComponent software)
    {
        _software = software;
        return this;
    }

    public CapabilityStatementBuilder WithImplementation(string description = null, string url = null, string custodian = null)
    {
        return WithImplementation(
            !string.IsNullOrWhiteSpace(description) ? new Markdown(description) : null,
            !string.IsNullOrWhiteSpace(url) ? new FhirUrl(url) : null,
            !string.IsNullOrWhiteSpace(custodian) ? new ResourceReference(custodian) : null
        );
    }
        
    public CapabilityStatementBuilder WithImplementation(Markdown description = null, FhirUrl url = null, ResourceReference custodian = null)
    {
        return WithImplementation(new ImplementationComponent
        {
            DescriptionElement = description,
            UrlElement = url,
            Custodian = custodian,
        });
    }
        
    public CapabilityStatementBuilder WithImplementation(ImplementationComponent implementation)
    {
        _implementation = implementation;
        return this;
    }

    public CapabilityStatementBuilder WithFhirVersion(FHIRVersion fhirVersion)
    {
        return WithFhirVersion(new Code<FHIRVersion>(fhirVersion));
    }
        
    public CapabilityStatementBuilder WithFhirVersion(Code<FHIRVersion> fhirVersion)
    {
        _fhirVersion = fhirVersion;
        return this;
    }

    public CapabilityStatementBuilder WithAcceptFormat(string format)
    {
        return WithAcceptFormat(new Code(format));
    }
        
    public CapabilityStatementBuilder WithAcceptFormat(Code format)
    {
        if (_format == null) _format = new List<Code>();

        _format.Add(format);
        return this;
    }
        
    public CapabilityStatementBuilder WithAcceptFormat(IEnumerable<string> format)
    {
        if (_format == null) _format = new List<Code>();
            
        _format.AddRange(format.Select(f => new Code(f)));

        return this;
    }

    public CapabilityStatementBuilder WithPatchFormat(string patchFormat)
    {
        return WithPatchFormat(new Code(patchFormat));
    }
        
    public CapabilityStatementBuilder WithPatchFormat(Code patchFormat)
    {
        if (_patchFormat == null) _patchFormat = new List<Code>();
        _patchFormat.Add(patchFormat);
        return this;
    }

    public CapabilityStatementBuilder WithImplementationGuide(string implementationGuide)
    {
        WithImplementationGuide(new Canonical(implementationGuide));
        return this;
    }

    public CapabilityStatementBuilder WithImplementationGuide(Canonical implementationGuide)
    {
        if (_implementationGuide == null) _implementationGuide = new List<Canonical>();
        _implementationGuide.Add(implementationGuide);
        return this;
    }

    public CapabilityStatementBuilder WithProfile(string profile)
    {
        return WithProfile(new ResourceReference(profile));
    }

    public CapabilityStatementBuilder WithProfile(ResourceReference profile)
    {
        if (_profile == null) _profile = new List<ResourceReference>();
        _profile.Add(profile);
        return this;
    }

    public CapabilityStatementBuilder WithRest(Func<RestComponent> configure)
    {
        return WithRest(configure());
    }
        
    public CapabilityStatementBuilder WithRest(RestComponent rest)
    {
        if (_rest == null) _rest = new List<RestComponent>();
        if(rest != null) _rest.Add(rest);
        return this;
    }

    public CapabilityStatementBuilder WithMessaging(Func<MessagingComponent> configure)
    {
        return WithMessaging(configure());
    }
        
    public CapabilityStatementBuilder WithMessaging(MessagingComponent messaging)
    {
        if (_messaging == null) _messaging = new List<MessagingComponent>();
        if(messaging != null) _messaging.Add(messaging);
        return this;
    }

    public CapabilityStatementBuilder WithDocument(DocumentMode mode, string profile, string documentation = null)
    {
        return WithDocument(mode, new Canonical(profile), !string.IsNullOrWhiteSpace(documentation) ? new Markdown(documentation) : null);
    }

    public CapabilityStatementBuilder WithDocument(DocumentMode mode, Canonical profile, Markdown documentation = null)
    {
        return WithDocument(new DocumentComponent()
        {
            Mode = mode,
            ProfileElement = profile,
            Documentation = documentation,
        });
    }

    public CapabilityStatementBuilder WithDocument(DocumentComponent document)
    {
        if (_document == null) _document = new List<DocumentComponent>();
        _document.Add(document);
        return this;
    }
}
