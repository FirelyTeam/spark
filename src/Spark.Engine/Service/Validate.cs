/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using Spark.Core;
using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Engine.Extensions;

namespace Spark.Service
{
    public static class Validate
    {
        public static void TypeName(string name)
        {

            if (ModelInfo.SupportedResources.Contains(name))
                return;

            //  Test for the most common mistake first: wrong casing of the resource name
            var correct = ModelInfo.SupportedResources.FirstOrDefault(s => s.ToUpperInvariant() == name.ToUpperInvariant());
            if (correct != null)
            {
                throw Error.NotFound("Wrong casing of collection name, try '{0}' instead", correct);
            }
            else
            {
                throw Error.NotFound("Unknown resource collection '{0}'", name);
            }
        }

        public static void ResourceType(IKey key, Resource resource)
        {
            if (resource == null)
                throw Error.BadRequest("Request did not contain a body");

            if (key.TypeName != resource.TypeName)
            {
                throw Error.BadRequest(
                    "Received a body with a '{0}' resource, which does not match the indicated collection '{1}' in the url.",
                    resource.TypeName, key.TypeName);
            }

        }

        public static void Key(IKey key)
        {
            if (key.HasResourceId())
            {
                Validate.ResourceId(key.ResourceId);
            }
            if (key.HasVersionId())
            {
                Validate.VersionId(key.VersionId);
            }
            if (!string.IsNullOrEmpty(key.TypeName))
            {
                Validate.TypeName(key.TypeName);
            }
        }

        public static void HasTypeName(IKey key)
        {
            if (string.IsNullOrEmpty(key.TypeName))
            {
                throw Error.BadRequest("Resource type is missing: {0}", key);
            }
        }

        public static void HasResourceId(IKey key)
        {
            if (key.HasResourceId())
            {
                Validate.ResourceId(key.ResourceId);
            }
            else
            {
                throw Error.BadRequest("The request should have a resource id.");
            }
        }

        public static void HasResourceId(Resource resource)
        {
            if (string.IsNullOrEmpty(resource.Id))
            {
                throw Error.BadRequest("The resource MUST contain an Id.");
            }
        }

        public static void IsResourceIdEqual(IKey key, Resource resource)
        {
            if (key.ResourceId != resource.Id)
            {
                throw Error.BadRequest("The Id in the request '{0}' is not the same is the Id in the resource '{1}'.", key.ResourceId, resource.Id);
            }
        }

        public static void HasVersion(IKey key)
        {
            if (key.HasVersionId())
            {
                Validate.VersionId(key.VersionId);
            }
            else 
            {
                throw Error.BadRequest("The request should contain a version id.");
            }
        }

        public static void HasNoVersion(IKey key)
        {
            if (key.HasVersionId())
            {
                throw Error.BadRequest("Resource should not contain a version.");
            }
        }

        public static void HasNoResourceId(IKey key)
        {
            if (key.HasResourceId())
            {
                throw Error.BadRequest("The request should not contain an id");
            }
        }

        public static void VersionId(string versionId)
        {
            if (String.IsNullOrEmpty(versionId))
            {
                throw Error.BadRequest("Must pass history id in url.");
            }
        }

        public static void ResourceId(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId))
            {
                throw Error.BadRequest("Logical ID is empty");
            }
            else if (!Id.IsValidValue(resourceId))
            {
                throw Error.BadRequest(String.Format("{0} is not a valid value for an id", resourceId));
            }
            else if (resourceId.Length > 64)
            {
                    throw Error.BadRequest("Logical ID is too long.");

            }
        }

        public static void IsSameVersion(IKey orignal, IKey replacement)
        {

            if (orignal.VersionId != replacement.VersionId)
            {
                throw Error.Create(HttpStatusCode.Conflict, "The current resource on this server '{0}' doesn't match the required version '{1}'", orignal, replacement);
            }

        
        }

        public static OperationOutcome AgainstModel(Resource resource)
        {
            // Phase 1, validate against low-level rules built into the FHIR datatypes

            /*
            (!FhirValidator.TryValidate(entry.Resource, vresults, recurse: true))
            {
                foreach (var vresult in vresults)
                    result.Issue.Add(createValidationResult("[.NET validation] " + vresult.ErrorMessage, vresult.MemberNames));
            }
            //doc.Validate(SchemaCollection.ValidationSchemaSet, 
            //    (source, args) => result.Issue.Add( createValidationResult("[XSD validation] " + args.Message,null)) 
            //);
            
            */
            throw new NotImplementedException();
        }

        public static OperationOutcome AgainstSchema(Resource resource)
        {
            OperationOutcome result = new OperationOutcome();
            result.Issue = new List<OperationOutcome.IssueComponent>();
            
            throw new NotImplementedException();
            
            //ICollection<ValidationResult> vresults = new List<ValidationResult>();

            // DSTU2: validation
            // Phase 2, validate against the XML schema
            
            //var xml = FhirSerializer.SerializeResourceToXml(resource);
            //var doc = XDocument.Parse(xml);
           
        }

        public static OperationOutcome AgainstProfile(Resource resource)
        {
            throw new NotImplementedException();

            /*
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
        }

        public static void Transaction(IList<Entry> interactions)
        {
            ValidateAllKeysUnique(interactions);
        }

        // The list of id's that have been reassigned. Maps from original id -> new id.
        private static IEnumerable<Uri> DoubleEntries(IEnumerable<Entry> entries)
        {
            // DSTU2: validation
            // moved from Importer

            // var keys = queue.Select(ent => ent.Key.ResourceId);
            //var selflinks = queue.Where(e => e.SelfLink != null).Select(e => e.SelfLink);
            //var all = keys.Concat(selflinks);

            //IEnumerable<Uri> doubles = all.GroupBy(u => u.ToString()).Where(g => g.Count() > 1).Select(g => g.First());

            //return doubles; 
            throw new NotImplementedException();
        }

        public static void ValidateAllKeysUnique(IList<Entry> interactions)
        {
            throw new NotImplementedException();
            // DSTU2: import
            //var doubles = DoubleEntries();
            //if (doubles.Count() > 0)
            //{
            //    string s = string.Join(", ", doubles);
            //    throw new SparkException("There are entries with duplicate SelfLinks or SelfLinks that are the same as an entry.Id: " + s);
            //}

        }

        public static void HasResourceType(IKey key, ResourceType type)
        {
            if (key.TypeName != Hacky.GetResourceNameForResourceType(type))
            {
                throw Error.BadRequest("Operation only valid for {0} resource type");
            }
        }
    }
}