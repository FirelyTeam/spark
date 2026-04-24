/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2020-2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Engine.Utility;

namespace Spark.Engine.Extensions;

internal static class ResourceExtensions
{
    private static bool IsValidResourcePath(string path, Resource resource)
    {
        string name = path.Split('.').FirstOrDefault();
        return resource.TypeName == name;
    }

    private static IEnumerable<string> GetReferences(this Resource resource, string path)
    {
        if (!IsValidResourcePath(path, resource))
            return [];

        if (StaticReferenceToFhirModel.FhirModel == null)
            throw new InvalidOperationException($"{nameof(StaticReferenceToFhirModel)}.FhirModel is not set.");

        var query = new ElementQuery(StaticReferenceToFhirModel.FhirModel, path);
        var list = new List<string>();

        query.Visit(resource, element =>
        {
            if (element is ResourceReference resourceReference)
            {
                string reference = resourceReference.Reference;
                if (reference != null)
                {
                    list.Add(reference);
                }
            }
        });
        return list;
    }

    private static IEnumerable<string> GetReferences(
        this IEnumerable<Resource> resources,
        string path)
    {
        return resources.SelectMany(resource => resource.GetReferences(path));
    }

    internal static IEnumerable<string> GetReferences(
        this IEnumerable<Resource> resources,
        IEnumerable<string> paths)
    {
        return paths.SelectMany(resources.GetReferences);
    }
}
