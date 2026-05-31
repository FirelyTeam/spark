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
    private static string MedicationRequestReplaceableConceptPath => "MedicationRequest.category";

    private static MedicationRequest CreateMedicationRequestWithActiveStatus()
        => new() { Id = "test", Status = MedicationRequest.MedicationRequestStatus.Active };

    private static void AssertMedicationRequestStatusCompleted(MedicationRequest resource)
        => Assert.Equal(MedicationRequest.MedicationRequestStatus.Completed, resource.Status);

    private static MedicationRequest CreateMedicationRequestWithReplaceableConcept()
        => new()
        {
            Id = "test",
            Category = new CodeableConcept
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
        => resource.Category;

    private static void AddVersionSpecificMedicationRequestPatch(Parameters parameters)
        => parameters.AddAddPatchParameter("MedicationRequest", "subject", new ResourceReference("abc"));

    private static void AssertVersionSpecificMedicationRequestPatch(MedicationRequest resource)
        => Assert.Equal("abc", resource.Subject?.Reference);

    private static string CreateAnnotationText(string text) => text;
}
