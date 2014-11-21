/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using Hl7.Fhir.Support;
using System.Xml;
using Hl7.Fhir.Model;
using System.Text.RegularExpressions;
using Spark.Support;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Rest;
using System.Diagnostics;
using Spark.Core;
//using SharpCompress.Archive.Zip;

namespace Spark.Support
{

    public struct Entry
    {
        public string Name;
        public ResourceFormat Format;
        public string Data;
    }
    internal class ExampleImporter
    {
		public Dictionary<string,List<BundleEntry>> ImportedEntries = new Dictionary<string,List<BundleEntry>>();
        
        public ResourceFormat FileResourceFormat(string filename)
        {
            string suffix = Path.GetExtension(filename).ToLower();
            switch(suffix) 
            {
                case ".xml": return ResourceFormat.Xml;
                case ".json":
                case ".js": return ResourceFormat.Json;
                default: return ResourceFormat.Unknown;
            }
       }


        private bool isFeed(string data)
        {
            if (data.Contains("<feed")) return true;
            if (data.Contains("resourceType") && data.Contains("Bundle")) return true;

            return false;
        }

        public void ImportFile(string filename)
        {
			// File may be in xml or json format
            ResourceFormat format = FileResourceFormat(filename);
            if (format == ResourceFormat.Unknown) 
                throw new ArgumentException(string.Format("File {0} does not end with suffix .xml, .json or .js", filename));

			string data = File.ReadAllText(filename);
            
            if (isFeed(data))
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

            ResourceEntry newEntry = ResourceEntry.Create(resource);
            newEntry.Resource = resource;
            newEntry.AuthorName = "(imported from file)";

            Match match = Regex.Match(filename, @"\w+\(([^\)]+)\)\..*");
            string name = match.Groups[1].Value;
            string id = (match.Success) ? match.Groups[1].Value : null;
            string collection = resource.GetCollectionName();

            if (id != null)
            {
                Uri identity = ResourceIdentity.Build(hl7base, collection, id);
                newEntry.Id = identity;
                newEntry.Title = String.Format("{0} with id {1}", collection, id) ;
            }
            else
            {
                newEntry.Title = String.Format("{0} from file {1}", collection, filename);
            }


            //identity = ResourceIdentity.Build(new Uri("http://hl7.org/fhir"), collection, id, "1");
            // identity.VersionId = "1";

            //newEntry.Links.SelfLink = identity;

            //newEntry.LastUpdated = File.GetLastWriteTimeUtc(filename);
            newEntry.Published = File.GetCreationTimeUtc(filename);

            add(newEntry);
        }


        private void FixKey(ResourceEntry entry)
        {
            if (!Key.IsValidExternalKey(entry.Id))
            {
                entry.Id = Key.NewCID();
            }

        }

        private void add(BundleEntry entry)
        {
			string name = null;

            if (entry is DeletedEntry)
            {
                name = "deleted";
            }
            else if (entry is ResourceEntry)
            {
                FixKey((ResourceEntry)entry);
                name = ((ResourceEntry)entry).Resource.GetCollectionName();
            }
            else
            {
                throw new ArgumentException("Cannot import BundleEntry of type " + entry.GetType().ToString());
            }
                

			List<BundleEntry> resources;
            
			if( ImportedEntries.ContainsKey(name) )
				resources = ImportedEntries[name];
            else
            {
				resources = new List<BundleEntry>();
                ImportedEntries.Add(name, resources);
            }
            
            resources.Add(entry);			
        }


        private void fixImportedEntryIfValueset(BundleEntry entry)
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
                    var name = "vs"+vsId.Substring(ix + 1);

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

        private void importBundle(string filename, Bundle bundle)
        {
            foreach (var entry in bundle.Entries)
            {
                if (entry is ResourceEntry)
                {
					// Make sure the Entry has its own author, even if it
					// is only specified on the container feed
					ResourceEntry ce = (ResourceEntry)entry;
                    if(ce.AuthorName == null) ce.AuthorName = ce.AuthorName;
                    if(ce.AuthorUri == null) ce.AuthorUri = ce.AuthorUri;
                }

                // Correct the id/selflink of the valueset if these are the included v2/v3
                // valuesets
                fixImportedEntryIfValueset(entry);

                add(entry);
            }
        }
        private static Resource tryParseResource(ResourceFormat format, string data)
        {
            Resource importedResource = null;
            //ErrorList errors = new ErrorList();
            if (format == ResourceFormat.Xml)
                importedResource = FhirParser.ParseResourceFromXml(data);
            if (format == ResourceFormat.Json)
                importedResource = FhirParser.ParseResourceFromJson(data);

            //if (errors.Count == 0)
            return importedResource;
            //else
            //    return null;
        }
        private static Bundle tryParseBundle(ResourceFormat format, string data)
        {
            Bundle importedBundle = null;
            //ErrorList errors = new ErrorList();
            if (format == ResourceFormat.Xml)
                importedBundle = FhirParser.ParseBundleFromXml(data);
            if (format == ResourceFormat.Json)
                importedBundle = FhirParser.ParseBundleFromJson(data);

            //if (errors.Count == 0)
                return importedBundle;
            //else
            //    return null;
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
            ResourceFormat format = FileResourceFormat(name);
            if (isFeed(data))
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
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
            using (FileStream zipFileToOpen = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (ZipArchive archive = new ZipArchive(zipFileToOpen, ZipArchiveMode.Read))
            {
                
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    StreamReader reader = new StreamReader(entry.Open());
                    string data = reader.ReadToEnd();
                    importData(entry.Name, data);
                }
            }
        }
    }
}
