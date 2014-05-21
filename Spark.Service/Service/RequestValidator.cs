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

namespace Spark.Controllers
{
    public static class RequestValidator
    {
        // todo: In Spark1 validation was written in and implemented at the rest level.
        // Here it's written in a seperate class and we want it implemented at either the rest or the service level, but this is not done yet.

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
                if (!IdPatternAttribute.IsValidValue(id))
                    throw new SparkException(HttpStatusCode.BadRequest, String.Format("{0} is not a valid value for an id", id));
            }
        }

        public static void ValidateCorrectUpdate(Uri mostRecentUpdateUri, Uri updatedUri)
        {
            var mostRecent = new ResourceIdentity(mostRecentUpdateUri);
            // If we require version-aware updates and no version to update was indicated,
            // we need to return a Precondition Failed.
            if (requiresVersionAwareUpdate(mostRecent.Collection) && updatedUri == null)
                throw new SparkException(HttpStatusCode.PreconditionFailed,
                    "This resource requires version-aware updates and no Content-Location was given");

            // Validate the updatedUri against the current version
            if (updatedUri != null)
            {
                var update = new ResourceIdentity(updatedUri);

                //if (mostRecent.OperationPath != update.OperationPath)
                if (mostRecent.VersionId != update.VersionId)
                {
                    throw new SparkException(HttpStatusCode.Conflict,
                        "There is an update conflict: update referred to version {0}, but current version is {1}",
                        update, mostRecent);
                }
            }
        }

        private static bool requiresVersionAwareUpdate(string collection)
        {
            // todo: question: Should this not be implemented somewhere else? (metadata?) /mh
            // answer: move to Config file.
            if (collection == "Organization")
                return true;
            else
                return false;
        }

        public static void ValidateResourceBody(ResourceEntry entry, string name)
        {
            if (entry==null || entry.Resource == null)
                throw new SparkException(HttpStatusCode.BadRequest, "Request did not contain a body");

            string collectionName = entry.Resource.GetCollectionName();
            if (name != collectionName)
            {
                throw new SparkException(HttpStatusCode.BadRequest,
                    "Received a body with a '{0}' resource, which does not match the indicated collection '{1}' in the url.", 
                            collectionName, name);
            }
        }

        public static void ValidateEntry(ResourceEntry entry)
        {
            try
            {
                FhirValidator.Validate(entry,recurse:true);
            }
            catch (Exception e)
            {
                throw new SparkException(HttpStatusCode.BadRequest, "Validation failed", e);
            }
        }

    }
}