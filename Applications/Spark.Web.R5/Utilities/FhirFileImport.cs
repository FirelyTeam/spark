/*
 * Copyright (c) 2019-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Utility;
using Spark.Engine.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Spark.Web.Utilities;

internal static class FhirFileImport
{
    private static Resource ParseResource(string data)
    {
        if (SerializationUtil.ProbeIsJson(data))
        {
            // TODO read config to determine if PermissiveParsing should be on 
            FhirJsonDeserializer parser = new(new DeserializerSettings().UsingMode(DeserializationMode.Recoverable));
            return parser.Deserialize<Resource>(data);
        }

        if (SerializationUtil.ProbeIsXml(data))
        {
            // TODO read config to determine if PermissiveParsing should be on 
            FhirXmlDeserializer parser = new(new DeserializerSettings().UsingMode(DeserializationMode.Recoverable));
            return parser.Deserialize<Resource>(data);
        }

        throw new FormatException("Data is neither Json nor Xml");
    }

    private static IEnumerable<Resource> ImportData(string data)
    {
        Resource resource = ParseResource(data);
        if (resource is Bundle bundle)
        {
            return bundle.GetResources();
        }

        return [resource];
    }

    private static IEnumerable<string> ExtractZipEntries(this byte[] buffer)
    {
        using Stream stream = new MemoryStream(buffer);
        using ZipArchive archive = new(stream, ZipArchiveMode.Read);
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            StreamReader reader = new(entry.Open());
            string data = reader.ReadToEnd();
            yield return data;
        }
    }

    private static IEnumerable<Resource> ExtractResourcesFromZip(this byte[] buffer)
    {
        return buffer.ExtractZipEntries().SelectMany(ImportData);
    }

    public static IEnumerable<Resource> ImportEmbeddedZip(string path)
    {
        return GetPathAsBytes(path).ExtractResourcesFromZip();
    }

    private static byte[] GetPathAsBytes(string path)
    {
        return File.ReadAllBytes(path);
    }

    public static Bundle ToBundle(this IEnumerable<Resource> resources)
    {
        Bundle bundle = new();
        foreach (Resource resource in resources)
        {
            bundle.Append(
                // POST resources without Ids, and PUT resources with Ids.
                resource.Id == null ? Bundle.HTTPVerb.POST : Bundle.HTTPVerb.PUT,
                resource
            );
        }

        return bundle;
    }
}
