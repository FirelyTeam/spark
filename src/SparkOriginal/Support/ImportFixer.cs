namespace Spark.Support
{
    public static class ImportFixer
    {
        
        //private void fixImportedEntryIfValueset(Entry entry)
        //{
        //    if (entry is ResourceEntry && ((ResourceEntry)entry).Resource is ValueSet)
        //    {
        //        string collectionName = typeof(ValueSet).GetCollectionName();

        //        var vs = (ResourceEntry<ValueSet>)entry;
        //        var vsId = vs.Id.ToString();
        //        //Debug.WriteLine(vsId);
        //        if (vsId.Contains("http://hl7.org/fhir/v2/vs"))
        //        {
        //            // http://hl7.org/fhir/vs/http://hl7.org/fhir/v2/vs/0006 (/2.1)
        //            int ix = vsId.LastIndexOf("v2/vs");
        //            var name = vsId.Substring(ix + 6);
        //            name = name.Replace('/', '-');

        //            entry.Id = ResourceIdentity.Build(new Uri("http://hl7.org/fhir"), collectionName, name);
        //            entry.SelfLink = ResourceIdentity.Build(new Uri("http://hl7.org/fhir"), collectionName, name, "1");
        //        }
        //        else if (vsId.Contains("http://hl7.org/fhir/v3/vs")) // http://hl7.org/fhir/v3/vs/ActCode
        //        {
        //            int ix = vsId.LastIndexOf("/");
        //            var name = "vs" + vsId.Substring(ix + 1);

        //            entry.Id = ResourceIdentity.Build(new Uri("http://hl7.org/fhir"), collectionName, name);
        //            entry.SelfLink = ResourceIdentity.Build(new Uri("http://hl7.org/fhir"), collectionName, name, "1");
        //        }
        //        else if (vsId.Contains("http://hl7.org/fhir/v3")) // http://hl7.org/fhir/v3/ActCode
        //        {
        //            int ix = vsId.LastIndexOf("/");
        //            var name = vsId.Substring(ix + 1);

        //            entry.Id = ResourceIdentity.Build(new Uri("http://hl7.org/fhir"), collectionName, name);
        //            entry.SelfLink = ResourceIdentity.Build(new Uri("http://hl7.org/fhir"), collectionName, name, "1");
        //        }
        //    }
        //}


        //private void importResource(string filename, Resource resource)
        //{
        //    System.Console.Out.WriteLine(filename + " is a single resource form filename: " + filename);

        //    var newEntry = Interaction.POST(resource);

        //    Match match = Regex.Match(filename, @"\w+\(([^\)]+)\)\..*");
        //    string name = match.Groups[1].Value;
        //    string id = (match.Success) ? match.Groups[1].Value : null;
        //    string collection = resource.TypeName;
        //    string versionid = "1";
        //    if (id != null)
        //    {
        //        newEntry.Key = Key.CreateLocal(collection, id, versionid);
        //    }
        //    newEntry.When = File.GetCreationTimeUtc(filename);

        //    add(newEntry);
        //}
    }
}