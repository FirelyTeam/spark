/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Validation;
using Spark.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using Spark.Core;
using System.ComponentModel.DataAnnotations;
using Hl7.Fhir.Serialization;
using System.Xml.Linq;
using System.Xml.Schema;
//using Hl7.Fhir.Profiling;
using System.Xml.XPath;
using System.IO;
//using Hl7.Fhir.Specification.Source;
//using Hl7.Fhir.Specification.Model;




namespace Spark.Controllers
{
    public enum ValidateOptions { None, NotVersioned };

    public static class RequestValidator
    {
        public static void ValidateCollectionName(string name)
        {
            if (ModelInfo.SupportedResources.Contains(name))
                return;

            // Error, now try the most common mistake: non-capitalized resource name
            var correct = ModelInfo.SupportedResources.FirstOrDefault(s => s.ToUpperInvariant() == name.ToUpperInvariant());
            if(correct != null)
                throw new SparkException(HttpStatusCode.NotFound, "Wrong casing of collection name, try '{0}' instead", correct);

            // Else, just fail
            throw new SparkException(HttpStatusCode.NotFound, "Unknown resource collection '{0}'", name);
        }

        public static void ValidateKey(Key key, ValidateOptions options = ValidateOptions.None)
        {
            if (string.IsNullOrEmpty(key.ResourceId))
                throw new SparkException(HttpStatusCode.BadRequest, "Logical ID is empty");
            
            if (key.ResourceId.Length > 36)
                throw new SparkException(HttpStatusCode.BadRequest, "Logical ID is too long.");

        }

        public static void ValidateId(string id)
        {
            if (String.IsNullOrEmpty(id))
                throw new SparkException(HttpStatusCode.BadRequest, "Must pass id in url.");

            ValidateIdPattern(id);
        }

        public static void ValidateVersionId(string version)
        {
            if (String.IsNullOrEmpty(version))
                throw new SparkException(HttpStatusCode.BadRequest, "Must pass history id in url.");

            ValidateIdPattern(version);
        }

        public static void ValidateIdPattern(string id)
        {
            if (id != null)
            {
                bool valid = Id.IsValidValue(id);   
                if (!valid)
                    throw new SparkException(HttpStatusCode.BadRequest, String.Format("{0} is not a valid value for an id", id));
            }
        }

        public static void ValidateVersion(Resource proposed, Resource current)
        {
            // todo: DSTU2

            //if (requiresVersionAwareUpdate(proposed))
            //{
            //    if (proposed.SelfLink == null)
            //        throw new SparkException(HttpStatusCode.PreconditionFailed,
            //        "This resource requires version-aware updates and no Content-Location was given");

            //    var _proposed = new ResourceIdentity(proposed.SelfLink);
            //    var _current = new ResourceIdentity(current.SelfLink);

            //    if (_proposed.VersionId != _current.VersionId)
            //    {
            //        throw new SparkException(HttpStatusCode.Conflict, "There is an update conflict: update referred to version {0}, but current version is {1}", _proposed, _current);
            //    }
            //}
        }

        private static bool requiresVersionAwareUpdate(Resource entry)
        {
            // todo: question: Should this not be implemented somewhere else? (metadata?) /mh
            // answer: move to Config file.
            return (entry.TypeName == "Organization");
        }

        public static void ValidateResourceBody(Resource resource, Key key)
        {
            if (resource==null)
                throw new SparkException(HttpStatusCode.BadRequest, "Request did not contain a body");

            if (key.TypeName != resource.TypeName)
            {
                throw new SparkException(HttpStatusCode.BadRequest,
                    "Received a body with a '{0}' resource, which does not match the indicated collection '{1}' in the url.", 
                            resource.TypeName, key.TypeName);
            }
        }

        public static OperationOutcome ValidateResource(Resource resource)
        {
            OperationOutcome result = new OperationOutcome();
            result.Issue = new List<OperationOutcome.OperationOutcomeIssueComponent>();

            ICollection<ValidationResult> vresults = new List<ValidationResult>();

            
            // Phase 1, validate against low-level rules built into the FHIR datatypes
           
            // todo: The API no longer seems to have the FhirValidator class.
            /*
            (!FhirValidator.TryValidate(entry.Resource, vresults, recurse: true))
            {
                foreach (var vresult in vresults)
                    result.Issue.Add(createValidationResult("[.NET validation] " + vresult.ErrorMessage, vresult.MemberNames));
            }
            */


            // todo: DSTU2
            // Phase 2, validate against the XML schema
            /*
            var xml = FhirSerializer.SerializeResourceToXml(entry.Resource);
            var doc = XDocument.Parse(xml);
            doc.Validate(SchemaCollection.ValidationSchemaSet, (source, args) => result.Issue.Add( createValidationResult("[XSD validation] " + args.Message,null) ));
            

            // Phase 3, validate against a profile, if present
            var profileTags = entry.GetAssertedProfiles();
            if (profileTags.Count() == 0)
            { 
                // If there's no profile specified, at least compare it to the "base" profile
                string baseProfile = CoreZipArtifactSource.CORE_SPEC_PROFILE_URI_PREFIX + entry.Resource.GetCollectionName();
                profileTags = new Uri[] { new Uri(baseProfile, UriKind.Absolute) };
            }

            var artifactSource = ArtifactResolver.CreateOffline();
            var specProvider = new SpecificationProvider(artifactSource);

            foreach (var profileTag in profileTags)
            {
                var specBuilder = new SpecificationBuilder(specProvider);
                specBuilder.Add(StructureFactory.PrimitiveTypes());
                specBuilder.Add(StructureFactory.MetaTypes());
                specBuilder.Add(StructureFactory.NonFhirNamespaces());
                specBuilder.Add(profileTag.ToString());
                specBuilder.Expand();

                string path = Directory.GetCurrentDirectory();

                var spec = specBuilder.ToSpecification();
                var nav = doc.CreateNavigator();
                nav.MoveToFirstChild();

                Report report = spec.Validate(nav);
                var errors = report.Errors;
                foreach (var error in errors)
                {
                    result.Issue.Add(createValidationResult("[Profile validator] " + error.Message, null));
                }
            }

            if(result.Issue.Count == 0)
                return null;
            else
                return result;
            */
            return null;
        }


        private static OperationOutcome.OperationOutcomeIssueComponent createValidationResult(string details, IEnumerable<string> location)
        {
            return new OperationOutcome.OperationOutcomeIssueComponent()
            {
                Severity = OperationOutcome.IssueSeverity.Error,
                Type = new Coding("http://hl7.org/fhir/issue-type", "invalid"),
                Details = details,
                Location = location
            };
        }

    }
}