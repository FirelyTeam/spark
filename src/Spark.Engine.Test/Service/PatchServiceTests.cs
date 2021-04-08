namespace Spark.Engine.Test.Service
{
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Engine.Extensions;
    using Engine.Service.FhirServiceExtensions;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Serialization;
    using Xunit;

    public class PatchServiceTests
    {
        private readonly FhirJsonParser _jsonParser = new FhirJsonParser();
        private readonly PatchService _applier = new PatchService();
        private readonly FhirXmlParser _xmlParser = new FhirXmlParser();

        [Fact]
        public void CanReplaceStatusOnMedicationRequest()
        {
            var json = File.ReadAllText(Path.Combine("TestData", "R4", "medication-status-replace-patch.json"));
            var parameters = _jsonParser.Parse<Parameters>(json);

            var resource = new MedicationRequest {Id = "test"};
            resource = (MedicationRequest) _applier.Apply(resource, parameters);

            Assert.Equal(MedicationRequest.medicationrequestStatus.Completed, resource.Status);
        }

        [Fact]
        public void CanReplacePerformerTypeOnMedicationRequest()
        {
            var resource = new MedicationRequest {Id = "test"};
            var json = File.ReadAllText(
                Path.Combine("TestData", "R4", "medication-replace-codeable-concept-patch.json"));
            var parameters = _jsonParser.Parse<Parameters>(json);

            resource = (MedicationRequest) _applier.Apply(resource, parameters);

            Assert.Equal("abc", resource.PerformerType.Coding[0].System);
            Assert.Equal("123", resource.PerformerType.Coding[0].Code);
            Assert.Equal("test", resource.PerformerType.Text);
        }

        [Fact]
        public void CanReplaceSubjectOnMedicationRequest()
        {
            var resource = new MedicationRequest {Id = "test", Subject = new ResourceReference("abc")};
            var json = File.ReadAllText(
                Path.Combine("TestData", "R4", "medication-replace-resource-reference-patch.json"));
            var parameters = _jsonParser.Parse<Parameters>(json);

            resource = (MedicationRequest) _applier.Apply(resource, parameters);

            Assert.Equal("abc", resource.Subject.Reference);
        }

        [Fact]
        public void CanReplaceInstantiatesCanonicalOnMedicationRequest()
        {
            var resource = new MedicationRequest {Id = "test"};
            var json = File.ReadAllText(Path.Combine("TestData", "R4", "medication-replace-canonical-patch.json"));
            var parameters = _jsonParser.Parse<Parameters>(json);

            resource = (MedicationRequest) _applier.Apply(resource, parameters);

            Assert.Equal("abc", resource.InstantiatesCanonical.First());
        }

        [Fact]
        public void CanReplaceDosageOnMedicationRequest()
        {
            var resource = new MedicationRequest {Id = "test"};
            var json = File.ReadAllText(
                Path.Combine("TestData", "R4", "medication-replace-dosage-instruction-patch.json"));
            var parameters = _jsonParser.Parse<Parameters>(json);

            resource = (MedicationRequest) _applier.Apply(resource, parameters);

            Assert.Equal(1m, resource.DosageInstruction[0].MaxDosePerLifetime.Value);
        }

        [Fact]
        public void CanApplyPropertyAssignmentPatch()
        {
            var xml = File.ReadAllText(Path.Combine("TestData", "R4", "property-assignment-patch.xml"));
            var parameters = _xmlParser.Parse<Parameters>(xml);

            var resource = new Patient {Id = "test"};
            resource = (Patient) _applier.Apply(resource, parameters);

            Assert.Equal("1930-01-01", resource.BirthDate);
        }

        [Fact]
        public void WhenApplyingPropertyAssignmentPatchToNonEmptyPropertyThenThrows()
        {
            var xml = File.ReadAllText(Path.Combine("TestData", "R4", "property-assignment-patch.xml"));
            var parameters = _xmlParser.Parse<Parameters>(xml);

            var resource = new Patient {Id = "test", BirthDate = "1930-01-01"};

            Assert.Throws<TargetInvocationException>(() => _applier.Apply(resource, parameters));
        }

        [Fact]
        public void CanApplyCollectionAddPatch()
        {
            var xml = File.ReadAllText(Path.Combine("TestData", "R4", "collection-add-patch.xml"));
            var parameters = _xmlParser.Parse<Parameters>(xml);

            var resource = new Patient {Id = "test"};
            resource = (Patient) _applier.Apply(resource, parameters);

            Assert.Equal("John", resource.Name[0].Given.First());
            Assert.Equal("Doe", resource.Name[0].Family);
        }

        [Fact]
        public void CanApplyCollectionReplacePatch()
        {
            var xml = File.ReadAllText(Path.Combine("TestData", "R4", "collection-replace-patch.xml"));
            var parameters = _xmlParser.Parse<Parameters>(xml);

            var resource = new Patient
            {
                Id = "test", Name = {new HumanName {Given = new[] {"John"}, Family = "Johnson"}}
            };
            resource = (Patient) _applier.Apply(resource, parameters);

            Assert.Equal("Jane", resource.Name[0].Given.First());
            Assert.Equal("Doe", resource.Name[0].Family);
        }

        [Fact]
        public void CanFullResourceReplacePatch()
        {
            var resource = new Patient
            {
                Id = "test", Name = {new HumanName {Given = new[] {"John"}, Family = "Johnson"}}
            };

            var replacement = new Patient
            {
                Id = "test", Name = {new HumanName {Given = new[] {"Jane"}, Family = "Doe"}}
            };

            var parameters = replacement.ToPatch();

            resource = (Patient) _applier.Apply(resource, parameters);

            Assert.Equal("Jane", resource.Name[0].Given.First());
            Assert.Equal("Doe", resource.Name[0].Family);
        }

        [Fact]
        public void CanCreateDiffPatch()
        {
            var resource = new Patient
            {
                Id = "test",
                Gender = AdministrativeGender.Male,
                Name = {new HumanName {Given = new[] {"John"}, Family = "Johnson"}}
            };

            var replacement = new Patient
            {
                Id = "test",
                BirthDateElement = new Hl7.Fhir.Model.Date(2020, 1, 2),
                Name = {new HumanName {Given = new[] {"Jane"}, Family = "Doe"}}
            };

            var parameters = replacement.ToPatch(resource);

            Assert.Equal(4, parameters.Parameter.Count);
        }

        [Fact]
        public void CanApplyCreatedDiffPatch()
        {
            var resource = new Patient
            {
                Id = "test",
                Gender = AdministrativeGender.Male,
                Name = {new HumanName {Given = new[] {"John"}, Family = "Johnson"}}
            };

            var replacement = new Patient
            {
                Id = "test",
                BirthDateElement = new Hl7.Fhir.Model.Date(2020, 1, 2),
                Name = {new HumanName {Given = new[] {"Jane"}, Family = "Doe"}}
            };

            var patch = replacement.ToPatch(resource);
            _applier.Apply(resource, patch);

            Assert.False(resource.Gender.HasValue);
            Assert.Equal(replacement.BirthDate, resource.BirthDate);
            Assert.Equal("Jane", resource.Name[0].Given.First());
            Assert.Equal("Doe", resource.Name[0].Family);
        }

        [Fact]
        public void CanApplyCollectionInsertPatch()
        {
            var xml = File.ReadAllText(Path.Combine("TestData", "R4", "collection-insert-patch.xml"));
            var parameters = _xmlParser.Parse<Parameters>(xml);

            var resource = new Patient
            {
                Id = "test", Name = {new HumanName {Given = new[] {"John"}, Family = "Johnson"}}
            };
            resource = (Patient) _applier.Apply(resource, parameters);

            Assert.Equal("Jane", resource.Name[0].Given.First());
            Assert.Equal("Doe", resource.Name[0].Family);

            Assert.Equal("John", resource.Name[1].Given.First());
            Assert.Equal("Johnson", resource.Name[1].Family);
        }

        [Fact]
        public void CanApplyCollectionMovePatch()
        {
            var xml = File.ReadAllText(Path.Combine("TestData", "R4", "collection-move-patch.xml"));
            var parameters = _xmlParser.Parse<Parameters>(xml);

            var resource = new Patient
            {
                Id = "test",
                Name =
                {
                    new HumanName {Given = new[] {"John"}, Family = "Johnson"},
                    new HumanName {Given = new[] {"Jane"}, Family = "Doe"}
                }
            };
            resource = (Patient) _applier.Apply(resource, parameters);

            Assert.Equal("Jane", resource.Name[0].Given.First());
            Assert.Equal("Doe", resource.Name[0].Family);

            Assert.Equal("John", resource.Name[1].Given.First());
            Assert.Equal("Johnson", resource.Name[1].Family);
        }

        [Fact]
        public void CanApplyPropertyReplacementPatch()
        {
            var xml = File.ReadAllText(Path.Combine("TestData", "R4", "property-replace-patch.xml"));
            var parameters = _xmlParser.Parse<Parameters>(xml);

            var resource = new Patient {Id = "test", BirthDate = "1970-12-24"};
            resource = (Patient) _applier.Apply(resource, parameters);

            Assert.Equal("1930-01-01", resource.BirthDate);
        }

        [Fact]
        public void CanApplyCollectionDeletePatch()
        {
            var xml = File.ReadAllText(Path.Combine("TestData", "R4", "collection-delete-patch.xml"));
            var parameters = _xmlParser.Parse<Parameters>(xml);

            var resource = new Patient {Id = "test", Name = {new HumanName {Text = "John Doe"}}};
            resource = (Patient) _applier.Apply(resource, parameters);

            Assert.Empty(resource.Name);
        }
    }
}
