/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Spark.Engine.Extensions;
using Xunit;

namespace Spark.Engine.Tests.Extensions;

public class ResourceExtensionsTests
{
    private const string SubsettedSystem = "http://terminology.hl7.org/CodeSystem/v3-ObservationValue";
    private const string SubsettedCode = "SUBSETTED";

    [Fact]
    public void MarkSubsetted_WithoutMeta_AddsMetaWithTheTag()
    {
        Patient patient = new();

        patient.MarkSubsetted();

        Assert.NotNull(patient.Meta);
        Coding tag = Assert.Single(patient.Meta.Tag);
        Assert.Equal(SubsettedSystem, tag.System);
        Assert.Equal(SubsettedCode, tag.Code);
        Assert.Equal("subsetted", tag.Display);
    }

    [Fact]
    public void MarkSubsetted_KeepsTheTagsAlreadyThere()
    {
        Patient patient = new() { Meta = new Meta { Tag = [new Coding("http://example.org/tags", "keep-me")] } };

        patient.MarkSubsetted();

        Assert.Equal(2, patient.Meta.Tag.Count);
        Assert.Contains(patient.Meta.Tag, tag => tag.Code == "keep-me");
        Assert.Contains(patient.Meta.Tag, tag => tag is { System: SubsettedSystem, Code: SubsettedCode });
    }

    [Fact]
    public void MarkSubsetted_CalledTwice_TagsOnlyOnce()
    {
        Patient patient = new();

        patient.MarkSubsetted();
        patient.MarkSubsetted();

        Assert.Single(patient.Meta.Tag);
    }

    [Fact]
    public void MarkSubsetted_LeavesContainedResourcesAlone()
    {
        Organization organization = new() { Id = "org" };
        Patient patient = new() { Contained = [organization] };

        patient.MarkSubsetted();

        Assert.Single(patient.Meta.Tag);
        Assert.True(organization.Meta == null || organization.Meta.Tag.Count == 0);
    }
}
