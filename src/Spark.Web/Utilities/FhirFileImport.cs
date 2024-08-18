/*
 * Copyright (c) 2019-2024, Incendi (info@incendi.no)
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

namespace Spark.Web.Utilities
{
	internal static class FhirFileImport
	{
		private static Resource ParseResource(string data)
		{
			if (SerializationUtil.ProbeIsJson(data))
			{
				// TODO read config to determine if PermissiveParsing should be on 
				FhirJsonParser parser = new FhirJsonParser(new ParserSettings { PermissiveParsing = true });
				return parser.Parse<Resource>(data);
			}
			else if (SerializationUtil.ProbeIsXml(data))
			{
				// TODO read config to determine if PermissiveParsing should be on 
				FhirXmlParser parser = new FhirXmlParser(new ParserSettings { PermissiveParsing = true });
				return parser.Parse<Resource>(data);
			}
			else
			{
				throw new FormatException("Data is neither Json nor Xml");
			}
		}

		public static IEnumerable<Resource> ImportData(string data)
		{
			Resource resource = ParseResource(data);
			if (resource is Bundle)
			{
				Bundle bundle = (resource as Bundle);
				return bundle.GetResources();
			}
			else
			{
				return new Resource[] { resource };
			}
		}

		public static IEnumerable<Resource> ImportFile(string filename)
		{
			string data = File.ReadAllText(filename);
			return ImportData(data);
		}

		public static IEnumerable<string> ExtractZipEntries(this byte[] buffer)
		{
			using (Stream stream = new MemoryStream(buffer))
			using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
			{
				foreach (ZipArchiveEntry entry in archive.Entries)
				{
					StreamReader reader = new StreamReader(entry.Open());
					string data = reader.ReadToEnd();
					yield return data;
				}
			}
		}

		public static IEnumerable<Resource> ExtractResourcesFromZip(this byte[] buffer)
		{
			return buffer.ExtractZipEntries().SelectMany(ImportData);
		}

		public static IEnumerable<Resource> ImportZip(string filename)
		{
			return File.ReadAllBytes(filename).ExtractZipEntries().SelectMany(ImportData); ;
		}

		public static IEnumerable<Resource> ImportEmbeddedZip(string path)
		{
			return GetPathAsBytes(path).ExtractResourcesFromZip();
		}

		public static byte[] GetPathAsBytes(string path)
		{
			return File.ReadAllBytes(path);
		}

		public static Bundle ToBundle(this IEnumerable<Resource> resources)
		{
			Bundle bundle = new Bundle();
			foreach (Resource resource in resources)
			{
				// Make sure that resources without id's are posted.
				if (resource.Id != null)
				{
					bundle.Append(Bundle.HTTPVerb.PUT, resource);
				}
				else
				{
					bundle.Append(Bundle.HTTPVerb.POST, resource);
				}
			}

			return bundle;
		}
	}
}