/* 
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Spark.Engine.Core;
using System.Linq;
using Xunit;
using static Hl7.Fhir.Model.CapabilityStatement;

namespace Spark.Engine.Tests.Core;

public partial class CapabilityStatementBuilderTests
{
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
        var capabilityStatement = WithVersionSpecificFhirVersion(new CapabilityStatementBuilder())
            .WithPublisher("Incendi")
            .WithVersion("1.5.7")
            .WithDate(new FhirDateTime(2021, 7, 4))
            .WithDescription("This FHIR SERVER is a reference Implementation server built in C# on HL7.Fhir.Core (nuget) by Firely, Incendi and others")
            .WithKind(CapabilityStatementKind.Instance)
            .WithAcceptFormat(FhirMediaType.JsonMimeTypes)
            .WithAcceptFormat(FhirMediaType.XmlMimeTypes)
            .WithRest(b => b
                .WithResource(r => r
                    .WithType("Patient")
                    .WithProfile("http://hl7.no/fhir/StructureDefinition/no-helseapi-Patient")
                    .WithInteraction(TypeRestfulInteraction.Read)
                    .WithInteraction(TypeRestfulInteraction.SearchType)
                    .WithSearchParam("identifier", SearchParamType.Token, documentation: "A patient identifier")
                    .WithSearchParam("name", SearchParamType.String, documentation: "A server defined search that may match any of the string fields in the HumanName, including family, give, prefix, suffix, suffix, and/or text")
                    .WithSearchParam("family", SearchParamType.String)
                    .WithSearchParam("given", SearchParamType.String)
                    .WithSearchParam("gender", SearchParamType.Token)
                )
                .WithResource(r => r
                    .WithType("Practitioner")
                    .WithProfile("http://hl7.no/fhir/StructureDefinition/no-helseapi-Practitioner")
                    .WithInteraction(TypeRestfulInteraction.Read)
                    .WithInteraction(TypeRestfulInteraction.SearchType)
                    .WithSearchParam("identifier", SearchParamType.Token, documentation: "A patient identifier")
                    .WithSearchParam("name", SearchParamType.String, documentation: "A server defined search that may match any of the string fields in the HumanName, including family, give, prefix, suffix, suffix, and/or text")
                    .WithSearchParam("family", SearchParamType.String)
                    .WithSearchParam("given", SearchParamType.String)
                )
                .WithResource(r => r
                    .WithType("DocumentReference")
                    .WithProfile("http://hl7.no/fhir/StructureDefinition/no-helseapi-DocumentReference")
                    .WithInteraction(TypeRestfulInteraction.Create)
                    .WithInteraction(TypeRestfulInteraction.Read)
                    .WithInteraction(TypeRestfulInteraction.SearchType)
                    .WithSearchParam("patient", SearchParamType.Reference, documentation: "The Person links to this Patient")
                    .WithSearchParam("type", SearchParamType.Token, documentation: "Kind of document")
                )
                .WithInteraction(SystemRestfulInteraction.Transaction)
            )
            .Build();
            
        Assert.Equal(1, capabilityStatement.Rest?.Count);
        Assert.Equal(3, capabilityStatement.Rest?.FirstOrDefault()?.Resource.Count);
        Assert.Equal(3, capabilityStatement.Rest?.FirstOrDefault()?.Resource.Count);
        Assert.Equal(5, capabilityStatement.Rest?.FirstOrDefault()?.Resource.Find(IsPatientResource)?.SearchParam?.Count);
        Assert.NotNull(capabilityStatement.Rest?.FirstOrDefault()?.Resource.Find(IsDocumentReferenceResource)?.Interaction.Find(interaction => interaction.Code == CapabilityStatement.TypeRestfulInteraction.Create));
    }
}
