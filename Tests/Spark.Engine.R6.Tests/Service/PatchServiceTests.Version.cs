/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Xunit;

namespace Spark.Engine.Tests;

public partial class PatchServiceTests
{
    private static string MedicationRequestReplaceableConceptPath => "MedicationRequest.performerType";

    private static MedicationRequest CreateMedicationRequestWithActiveStatus()
        => new() { Id = "test", Status = MedicationRequest.MedicationrequestStatus.Active };

    private static void AssertMedicationRequestStatusCompleted(MedicationRequest resource)
        => Assert.Equal(MedicationRequest.MedicationrequestStatus.Completed, resource.Status);

    private static MedicationRequest CreateMedicationRequestWithReplaceableConcept()
        => new()
        {
            Id = "test",
            PerformerType = new CodeableConcept
            {
                Coding =
                [
                    new Coding
                    {
                        System = "abc",
                        Code = "123",
                    },
                ],
                Text = "test1",
            },
        };

    private static CodeableConcept GetMedicationRequestReplaceableConcept(MedicationRequest resource)
        => resource.PerformerType;

    private static void AddVersionSpecificMedicationRequestPatch(Parameters parameters)
        => parameters.AddAddPatchParameter("MedicationRequest", "renderedDosageInstruction", new Markdown("Take once daily"));

    private static void AssertVersionSpecificMedicationRequestPatch(MedicationRequest resource)
        => Assert.Equal("Take once daily", resource.RenderedDosageInstruction);

    private static Markdown CreateAnnotationText(string text) => new(text);
}
