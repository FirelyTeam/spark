using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System.IO;
using System.IO.Compression;
using Spark.Engine.Extensions;

namespace Spark.Support
{
    internal static class FhirLoader
    {
        private static Resource ParseResource(string data)
        {
            if (FhirParser.ProbeIsJson(data))
            {
                return FhirParser.ParseResourceFromJson(data);
            }
            else if (FhirParser.ProbeIsXml(data))
            {
                return FhirParser.ParseResourceFromXml(data);
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

        public static IEnumerable<Resource> ImportDirectory(string dirname)
        {
            if (!Directory.Exists(dirname))
                throw new DirectoryNotFoundException(String.Format("Cannot import from directory {0}: not found or not a directory", dirname));

            return Directory.EnumerateFiles(dirname).SelectMany(ImportFile);
        }

        public static IEnumerable<Resource> ExtractAndImportZip(string filename)
        {
            string dirName = "FhirImport-" + Guid.NewGuid().ToString();
            string tempDir = Path.Combine(Path.GetTempPath(), dirName);
            ZipFile.ExtractToDirectory(filename, tempDir);

            return ImportDirectory(tempDir);
        }

    }


    
}