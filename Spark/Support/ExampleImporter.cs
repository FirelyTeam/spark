/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;

using System.Xml;
using System.Text.RegularExpressions;

using Hl7.Fhir.Support;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Rest;

using Spark.Support;
using Spark;
using Spark.Core;
using Spark.Service;

//using SharpCompress.Archive.Zip;

namespace Spark.Support
{
    
    internal class FhirZipImporter
    {
        List<Interaction> entries = new List<Interaction>();
        
        public void ImportFile(string filename)
        {
            // File may be in xml or json format
            ResourceFormat format = FhirData.GetFileFormat(filename);
            if (format == ResourceFormat.Unknown)
                throw new ArgumentException(string.Format("File {0} does not end with suffix .xml, .json or .js", filename));

            string data = File.ReadAllText(filename);

            if (FhirData.IsFeed(data))
            {
                Bundle importedBundle = tryParseBundle(format, data);
                importBundle(filename, importedBundle);
            }
            else
            {
                Resource importedResource = tryParseResource(format, data);
                importResource(filename, importedResource);
            }
        }

        private static Uri hl7base = new Uri("http://hl7.org/fhir");

        private void importResource(string filename, Resource resource)
        {
            System.Console.Out.WriteLine(filename + " is a single resource form filename: " + filename);

            Interaction newEntry = new Interaction(resource);
            

            Match match = Regex.Match(filename, @"\w+\(([^\)]+)\)\..*");
            string name = match.Groups[1].Value;
            string id = (match.Success) ? match.Groups[1].Value : null;
            string collection = resource.TypeName;
            string versionid = "1";
            if (id != null)
            {
                newEntry.Key = Key.CreateLocal(collection, id, versionid);
            }
            newEntry.When = File.GetCreationTimeUtc(filename);

            add(newEntry);
        }

        private void fixKey(Interaction entry)
        {
            IKey key = entry.Key;
            if (!key.HasResourceId)
            {
                key.ResourceId = UriHelper.CreateCID();
                entry.Key = key;
            }
        }

        private void add(Interaction entry)
        {
            string name = null;
            if (entry.Resource != null)
            {
                fixKey(entry);
                name = entry.Key.TypeName;
            }
            else
            {
                throw new ArgumentException("Cannot import BundleEntry of type " + entry.GetType().ToString());
            }
            entries.Add(entry);
        }


        // DSTU2: import
        /*
        private void fixImportedEntryIfValueset(Entry entry)
        {
            if (entry is ResourceEntry && ((ResourceEntry)entry).Resource is ValueSet)
            {
                string collectionName = typeof(ValueSet).GetCollectionName();

                var vs = (ResourceEntry<ValueSet>)entry;
                var vsId = vs.Id.ToString();
                //Debug.WriteLine(vsId);
                if (vsId.Contains("http://hl7.org/fhir/v2/vs"))
                {
                    // http://hl7.org/fhir/vs/http://hl7.org/fhir/v2/vs/0006 (/2.1)
                    int ix = vsId.LastIndexOf("v2/vs");
                    var name = vsId.Substring(ix + 6);
                    name = name.Replace('/', '-');

                    entry.Id = ResourceIdentity.Build(new Uri("http://hl7.org/fhir"), collectionName, name);
                    entry.SelfLink = ResourceIdentity.Build(new Uri("http://hl7.org/fhir"), collectionName, name, "1");
                }
                else if (vsId.Contains("http://hl7.org/fhir/v3/vs")) // http://hl7.org/fhir/v3/vs/ActCode
                {
                    int ix = vsId.LastIndexOf("/");
                    var name = "vs" + vsId.Substring(ix + 1);

                    entry.Id = ResourceIdentity.Build(new Uri("http://hl7.org/fhir"), collectionName, name);
                    entry.SelfLink = ResourceIdentity.Build(new Uri("http://hl7.org/fhir"), collectionName, name, "1");
                }
                else if (vsId.Contains("http://hl7.org/fhir/v3")) // http://hl7.org/fhir/v3/ActCode
                {
                    int ix = vsId.LastIndexOf("/");
                    var name = vsId.Substring(ix + 1);

                    entry.Id = ResourceIdentity.Build(new Uri("http://hl7.org/fhir"), collectionName, name);
                    entry.SelfLink = ResourceIdentity.Build(new Uri("http://hl7.org/fhir"), collectionName, name, "1");
                }
            }
        }
        */

        private void importBundle(string filename, Bundle bundle)
        {
            foreach (var bundleentry in bundle.Entry)
            {
                Interaction entry = bundleentry.TranslateToInteraction();


                // DSTU2: import
                
                // Correct the id/selflink of the valueset if these are the included v2/v3 valuesets
                // fixImportedEntryIfValueset(entry);

                add(entry);
            }
        }

        private static Resource tryParseResource(ResourceFormat format, string data)
        {
            Resource importedResource = null;

            if (format == ResourceFormat.Xml)
                importedResource = FhirParser.ParseResourceFromXml(data);
            if (format == ResourceFormat.Json)
                importedResource = FhirParser.ParseResourceFromJson(data);
            
            return importedResource;
        }

        private static Bundle tryParseBundle(ResourceFormat format, string data)
        {
            Bundle importedBundle = null;

            if (format == ResourceFormat.Xml)
                importedBundle = (Bundle)FhirParser.ParseResourceFromXml(data);

            if (format == ResourceFormat.Json)
                importedBundle = (Bundle)FhirParser.ParseResourceFromJson(data);

            return importedBundle;
        }

        public void ImportDirectory(string dirname)
        {
            if (!Directory.Exists(dirname))
                throw new DirectoryNotFoundException(String.Format("Cannot import from directory {0}: not found or not a directory", dirname));

            foreach (var file in Directory.EnumerateFiles(dirname))
                ImportFile(file);
        }

        public void ExtractAndImportZip(string filename)
        {
            string dirName = "FhirImport-" + Guid.NewGuid().ToString();
            string tempDir = Path.Combine(Path.GetTempPath(), dirName);
            ZipFile.ExtractToDirectory(filename, tempDir);

            ImportDirectory(tempDir);
        }

        private void importData(string name, string data)
        {
            ResourceFormat format = FhirData.GetFileFormat(name);
            if (FhirData.IsFeed(data))
            {
                Bundle importedBundle = tryParseBundle(format, data);
                importBundle(name, importedBundle);
            }
            else
            {
                Resource importedResource = tryParseResource(format, data);
                importResource(name, importedResource);
            }
        }

        public void ImportZip(string filename)
        {
            byte[] buffer = Spark.Resources.Resources.ExamplesZip;
            
            using (Stream stream = new MemoryStream(buffer))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    StreamReader reader = new StreamReader(entry.Open());
                    string data = reader.ReadToEnd();
                    importData(entry.Name, data);
                }
            }
        }

        public static IEnumerable<Interaction> Unzip(string zipfile)
        {
            var importer = new FhirZipImporter();
            importer.ImportZip(zipfile);
            return importer.entries;
        }

        public static Bundle UnzipAsBundle(string zipfile)
        {
            var entries = FhirZipImporter.Unzip(zipfile);
            var bundle = BundleFactory.Create("Imported examples", null, entries);
            return bundle;
        }

    }

    internal static class FhirData
    {
        public static ResourceFormat GetFileFormat(string filename)
        {
            string suffix = Path.GetExtension(filename).ToLower();
            switch (suffix)
            {
                case ".xml": return ResourceFormat.Xml;
                case ".json":
                case ".js": return ResourceFormat.Json;
                default: return ResourceFormat.Unknown;
            }
        }

        public static bool IsFeed(string data)
        {
            if (data.Contains("<feed")) return true;
            if (data.Contains("resourceType") && data.Contains("Bundle")) return true;

            return false;
        }
    }
}
