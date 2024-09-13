/* 
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.Linq;
using System.Net;
using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;
using Spark.Engine.Core;
using Error = Spark.Core.Error;

namespace Spark.Service;

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

    public static void ValidateKey(IKey key, bool withVersion = false)
    {
        Validate.HasTypeName(key);
        Validate.HasResourceId(key);
        if (withVersion)
        {
            Validate.HasVersion(key);
        }
        else
        {
            Validate.HasNoVersion(key);
        }
    }

    public static void Key(IKey key)
    {
        if (key.HasResourceId())
        {
            ResourceId(key.ResourceId);
        }
        if (key.HasVersionId())
        {
            VersionId(key.VersionId);
        }
        if (!string.IsNullOrEmpty(key.TypeName))
        {
            TypeName(key.TypeName);
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
            ResourceId(key.ResourceId);
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
            VersionId(key.VersionId);
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
        if (string.IsNullOrEmpty(versionId))
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
            throw Error.BadRequest(string.Format("{0} is not a valid value for an id", resourceId));
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

    public static void HasResourceType(IKey key, ResourceType type)
    {
        if (key.TypeName != EnumUtility.GetLiteral(type))
        {
            throw Error.BadRequest("Operation only valid for {0} resource type");
        }
    }
}