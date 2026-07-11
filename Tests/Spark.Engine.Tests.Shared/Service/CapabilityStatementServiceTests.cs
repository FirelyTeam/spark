/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Moq;
using Spark.Engine.Core;
using Spark.Engine.Service.FhirServiceExtensions;
using Xunit;
using static Hl7.Fhir.Model.CapabilityStatement;

namespace Spark.Engine.Tests.Service;

public partial class CapabilityStatementServiceTests
{
    private static readonly string[] SupportedResources = ["Patient", "Observation"];

    private static readonly IReadOnlyList<Model.SearchParameter> PatientSearchParams =
    [
        new() { Name = "name", Type = SearchParamType.String, Description = "Patient name" },
        new() { Name = "birthdate", Type = SearchParamType.Date, Description = "Patient birthdate" },
    ];

    private static readonly IReadOnlyList<Spark.Engine.Model.SearchParameter> ObservationSearchParams =
    [
        new() { Name = "code", Type = SearchParamType.Token, Description = "Observation code" },
    ];

    private static CapabilityStatementService CreateService(Uri baseUri = null)
    {
        baseUri ??= new Uri("http://localhost/fhir");

        var mockLocalhost = new Mock<ILocalhost>();
        mockLocalhost
            .Setup(l => l.Absolute(It.IsAny<Uri>()))
            .Returns<Uri>(relative => new Uri(baseUri, relative));

        var mockFhirModel = new Mock<IFhirModel>();
        mockFhirModel
            .Setup(m => m.SupportedResources)
            .Returns(SupportedResources);
        mockFhirModel
            .Setup(m => m.FindSearchParameters("Patient"))
            .Returns(PatientSearchParams);
        mockFhirModel
            .Setup(m => m.FindSearchParameters("Observation"))
            .Returns(ObservationSearchParams);

        var serverVersion = new ServerVersion(2, 0, 0);

        return new CapabilityStatementService(
            mockLocalhost.Object,
            mockFhirModel.Object,
            serverVersion,
            FHIRVersion.N4_0_1);
    }

    [Fact]
    public void GetSparkCapabilityStatement_ReturnsSameInstance()
    {
        var service = CreateService();
        var first = service.GetSparkCapabilityStatement();
        var second = service.GetSparkCapabilityStatement();
        Assert.Same(first, second);
    }

    [Fact]
    public void GetSparkCapabilityStatement_HasExpectedMetadata()
    {
        var service = CreateService();
        var cs = service.GetSparkCapabilityStatement();

        Assert.Equal("Spark FHIR Server", cs.Name);
        Assert.Equal("Incendi", cs.Publisher);
        Assert.Equal("2.0.0", cs.Version);
        Assert.True(cs.Experimental);
        Assert.Equal(CapabilityStatementKind.Capability, cs.Kind.GetValueOrDefault());
        Assert.Equal(new[] { "xml", "json" }, cs.Format.ToArray());
    }

    [Fact]
    public void GetSparkCapabilityStatement_HasCopyright()
    {
        var service = CreateService();
        var cs = service.GetSparkCapabilityStatement();
        Assert.Contains("Open Source Software", cs.Copyright ?? "");
    }

    [Fact]
    public void GetSparkCapabilityStatement_HasOneRestComponent()
    {
        var service = CreateService();
        var cs = service.GetSparkCapabilityStatement();
        Assert.Single(cs.Rest);
        Assert.Equal(RestfulCapabilityMode.Server, cs.Rest[0].Mode.GetValueOrDefault());
    }

    [Fact]
    public void GetSparkCapabilityStatement_HasAllSupportedResources()
    {
        var service = CreateService();
        var cs = service.GetSparkCapabilityStatement();
        var resourceTypes = cs.Rest[0].Resource.Select(r => r.Type).ToArray();
        Assert.Equal(SupportedResources.Length, resourceTypes.Length);
        Assert.True(ContainsPatientResource(resourceTypes));
        Assert.True(ContainsObservationResource(resourceTypes));
    }

    [Fact]
    public void GetSparkCapabilityStatement_EachResourceHasAllTypeRestfulInteractions()
    {
        var service = CreateService();
        var cs = service.GetSparkCapabilityStatement();
        var allInteractions = Enum.GetValues(typeof(TypeRestfulInteraction)).Cast<TypeRestfulInteraction>().ToHashSet();

        foreach (var resource in cs.Rest[0].Resource)
        {
            var resourceInteractions = resource.Interaction.Select(i => i.Code.GetValueOrDefault()).ToHashSet();
            Assert.Equal(allInteractions, resourceInteractions);
        }
    }

    [Fact]
    public void GetSparkCapabilityStatement_EachResourceHasVersioningAndFlags()
    {
        var service = CreateService();
        var cs = service.GetSparkCapabilityStatement();

        foreach (var resource in cs.Rest[0].Resource)
        {
            Assert.Equal(ResourceVersionPolicy.VersionedUpdate, resource.Versioning.GetValueOrDefault());
            Assert.True(resource.ReadHistory);
            Assert.True(resource.UpdateCreate);
        }
    }

    [Fact]
    public void GetSparkCapabilityStatement_PatientHasExpectedSearchParams()
    {
        var service = CreateService();
        var cs = service.GetSparkCapabilityStatement();
        var patient = cs.Rest[0].Resource.Single(IsPatientResource);
        var paramNames = patient.SearchParam.Select(sp => sp.Name).ToHashSet();

        Assert.Contains("name", paramNames);
        Assert.Contains("birthdate", paramNames);
        Assert.Contains(GeneralParameters.Summary, paramNames);
    }

    [Fact]
    public void GetSparkCapabilityStatement_HasSummarySearchParamOnEveryResource()
    {
        var service = CreateService();
        var cs = service.GetSparkCapabilityStatement();

        foreach (var resource in cs.Rest[0].Resource)
        {
            var names = resource.SearchParam.Select(sp => sp.Name);
            Assert.Contains(GeneralParameters.Summary, names);
        }
    }

    [Fact]
    public void GetSparkCapabilityStatement_HasExpectedSystemInteractions()
    {
        var service = CreateService();
        var cs = service.GetSparkCapabilityStatement();
        var interactions = cs.Rest[0].Interaction.Select(i => i.Code.GetValueOrDefault()).ToHashSet();

        Assert.Contains(SystemRestfulInteraction.Transaction, interactions);
        Assert.Contains(SystemRestfulInteraction.Batch, interactions);
        Assert.Contains(SystemRestfulInteraction.SearchSystem, interactions);
        Assert.Contains(SystemRestfulInteraction.HistorySystem, interactions);
    }

    [Fact]
    public void GetSparkCapabilityStatement_HasExpectedOperations()
    {
        var service = CreateService(new Uri("http://localhost/fhir/"));
        var cs = service.GetSparkCapabilityStatement();
        var operations = cs.Rest[0].Operation.ToDictionary(o => o.Definition, o => o.Name);

        Assert.True(ContainsOperationDefinition("OperationDefinition/Patient-everything", operations));
        Assert.True(ContainsOperationDefinition("OperationDefinition/Composition-document", operations));
        Assert.Contains("Fetch Patient Record", operations.Values);
        Assert.Contains("Generate a Document", operations.Values);
    }

    [Fact]
    public void GetSparkCapabilityStatement_CanBeSerializedAndDeserializedInStrictMode()
    {
        var service = CreateService(new Uri("http://localhost/fhir/"));
        var capabilityStatement = service.GetSparkCapabilityStatement();

        var serializer = new FhirJsonSerializer();
        var serializedCapabilityStatement = serializer.SerializeToString(capabilityStatement);

        var deserializer = new FhirJsonDeserializer(new DeserializerSettings().UsingMode(DeserializationMode.Strict));
        _ = deserializer.Deserialize<CapabilityStatement>(serializedCapabilityStatement);
    }
}
