/* 
 * Copyright (c) 2021-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Spark.Engine.Core;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using static Hl7.Fhir.Model.CapabilityStatement;

namespace Spark.Engine.Test.Core;

public class CapabilityStatementBuilderTests
{
    private readonly ITestOutputHelper _output;

    public CapabilityStatementBuilderTests(ITestOutputHelper output)
    {
        _output = output;
    }
        
    [Fact]
    public void CapabilityStatementStatusIsActive()
    {
        var builder = new CapabilityStatementBuilder();
        var capabilityStatement = builder
            .WithStatus(PublicationStatus.Active)
            .Build();
        Assert.Equal(PublicationStatus.Active, capabilityStatement.Status);
    }

    [Fact]
    public void CapabilityStatementStatusIsDraft()
    {
        var builder = new CapabilityStatementBuilder();
        var capabilityStatement = builder
            .WithStatus(PublicationStatus.Draft)
            .Build();
        Assert.Equal(PublicationStatus.Draft, capabilityStatement.Status);
    }

    [Fact]
    public void CapabilityStatementNameIsCS_SPARK()
    {
        var builder = new CapabilityStatementBuilder();
        var capabilityStatement = builder
            .WithName("CS_SPARK")
            .Build();
        Assert.Equal("CS_SPARK", capabilityStatement.Name);
    }

    [Fact]
    public void CapabilityStatementTitleIsSpark_Capability_Statement()
    {
        var builder = new CapabilityStatementBuilder();
        var capabilityStatement = builder
            .WithTitle("Spark Capability Statement")
            .Build();
        Assert.Equal("Spark Capability Statement", capabilityStatement.Title);
    }
        
    [Fact]
    public void CapabilityStatementCanBuildRestComponent()
    {
        var builder = new CapabilityStatementBuilder();
        var capabilityStatement = builder
            .WithPublisher("Incendi")
            .WithVersion("1.5.7")
            .WithDate(new FhirDateTime(2021, 7, 4))
            .WithDescription("This FHIR SERVER is a reference Implementation server built in C# on HL7.Fhir.Core (nuget) by Firely, Incendi and others")
            .WithKind(CapabilityStatementKind.Instance)
            .WithFhirVersion(FHIRVersion.N4_0_1)
            .WithAcceptFormat(FhirMediaType.JsonMimeTypes)
            .WithAcceptFormat(FhirMediaType.XmlMimeTypes)
            .WithRest(() => 
                new RestComponentBuilder()
                    .WithResource(() => new ResourceComponent
                    {
                        Type = "Patient",
                        Profile = "http://hl7.no/fhir/StructureDefinition/no-helseapi-Patient",
                        Interaction = new List<ResourceInteractionComponent>
                        {
                            new ResourceInteractionComponent {Code = TypeRestfulInteraction.Read},
                            new ResourceInteractionComponent {Code = TypeRestfulInteraction.SearchType},
                        },
                        SearchParam = new List<SearchParamComponent>
                        {
                            new SearchParamComponent {Name = "identifier", Type = SearchParamType.Token, Documentation = new Markdown("A patient identifier")},
                            new SearchParamComponent {Name = "name", Type = SearchParamType.String, Documentation = new Markdown("A server defined search that may match any of the string fields in the HumanName, including family, give, prefix, suffix, suffix, and/or text")},
                            new SearchParamComponent {Name = "family", Type = SearchParamType.String},
                            new SearchParamComponent {Name = "given", Type = SearchParamType.String},
                            new SearchParamComponent {Name = "gender", Type = SearchParamType.Token},
                        },
                    })
                    .WithResource(() => new ResourceComponent
                    {
                        Type = "Practitioner",
                        Profile = "http://hl7.no/fhir/StructureDefinition/no-helseapi-Practitioner",
                        Interaction = new List<ResourceInteractionComponent>
                        {
                            new ResourceInteractionComponent {Code = TypeRestfulInteraction.Read},
                            new ResourceInteractionComponent {Code = TypeRestfulInteraction.SearchType},
                        },
                        SearchParam = new List<SearchParamComponent>
                        {
                            new SearchParamComponent {Name = "identifier", Type = SearchParamType.Token, Documentation = new Markdown("A patient identifier")},
                            new SearchParamComponent {Name = "name", Type = SearchParamType.String, Documentation = new Markdown("A server defined search that may match any of the string fields in the HumanName, including family, give, prefix, suffix, suffix, and/or text")},
                            new SearchParamComponent {Name = "family", Type = SearchParamType.String},
                            new SearchParamComponent {Name = "given", Type = SearchParamType.String},
                        },
                    })
                    .WithResource(() => new ResourceComponent
                    {
                        Type = "DocumentReference",
                        Profile = "http://hl7.no/fhir/StructureDefinition/no-helseapi-DocumentReference",
                        Interaction = new List<ResourceInteractionComponent>
                        {
                            new ResourceInteractionComponent {Code = TypeRestfulInteraction.Create},
                            new ResourceInteractionComponent {Code = TypeRestfulInteraction.Read},
                            new ResourceInteractionComponent {Code = TypeRestfulInteraction.SearchType},
                        },
                        SearchParam = new List<SearchParamComponent>
                        {
                            new SearchParamComponent {Name = "patient", Type = SearchParamType.Reference, Documentation = new Markdown("The Person links to this Patient")},
                            new SearchParamComponent {Name = "type", Type = SearchParamType.Token, Documentation = new Markdown("Kind of document")},
                        },
                    })
                    .WithInteraction(SystemRestfulInteraction.Transaction)
                    .Build()
            )
            .Build();
            
        Assert.Equal(1, capabilityStatement.Rest?.Count);
        Assert.Equal(3, capabilityStatement.Rest?.FirstOrDefault()?.Resource.Count);
        Assert.Equal(3, capabilityStatement.Rest?.FirstOrDefault()?.Resource.Count);
        Assert.Equal(5, capabilityStatement.Rest?.FirstOrDefault()?.Resource.Find(rest => rest.Type == "Patient")?.SearchParam?.Count);
        Assert.NotNull(capabilityStatement.Rest?.FirstOrDefault()?.Resource.Find(rest => rest.Type == "DocumentReference")?.Interaction.Find(interaction => interaction.Code == CapabilityStatement.TypeRestfulInteraction.Create));
    }
}
