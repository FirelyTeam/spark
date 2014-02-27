using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Service;
using Hl7.Fhir.Support;
using Hl7.Fhir.Model;
using Spark.Data.MongoDB;
using Spark.Data.AmazonS3;
using Spark.Support;

namespace SparkTests
{
    [TestClass]
    public class TestBatchHandling
    {
        private MongoFhirStore _store = null;

        [TestInitialize]
        public void Setup()
        {
            
            _store = new MongoFhirStore();
            _store.EraseData();
        }

        [TestMethod]
        public void TestIdReassignOnImport()
        {
            var importer = new ResourceImporter(new Uri("http://fhir.furore.com/fhir/"));
            importer.AddSharedIdSpacePrefix("http://localhost");

            importer.QueueNewResourceEntry(new Uri("http://someserver.nl/fhir/patient/@1012"), new Patient());
            importer.QueueNewResourceEntry(new Uri("http://fhir.furore.com/fhir/patient/@13"), new Patient());
            importer.QueueNewResourceEntry(new Uri("http://localhost/svc/patient/@24"), new Patient());
            importer.QueueNewResourceEntry(new Uri("cid:332211223316"), new Patient());
            importer.QueueNewDeletedEntry(new Uri("http://someserver.nl/fhir/patient/@17"));
            var imported = importer.ImportQueued();

            Assert.AreEqual("patient/@1", imported.First().Id.ToString());
            Assert.AreEqual("patient/@13", imported.Skip(1).First().Id.ToString());
            Assert.AreEqual("patient/@24", imported.Skip(2).First().Id.ToString());
            Assert.AreEqual("patient/@25", imported.Skip(3).First().Id.ToString());
            Assert.AreEqual("patient/@26", imported.Skip(4).First().Id.ToString());
        }
       

        [TestMethod]
        public void TestUriReassignments()
        {
            Patient p = new Patient();

            p.Link = new List<ResourceReference>() { new ResourceReference { Type = "patient", Reference = "patient/@10" } };
            p.Provider = new ResourceReference() { Type = "organization", Reference = "http://outside.com/fhir/organization/@100" };
            p.Contact = new List<Patient.ContactComponent>() {
                new Patient.ContactComponent() { 
                    Organization = new ResourceReference() { Type = "organization", Reference = "http://hl7.org/fhir/organization/@200" } } };
            p.Photo = new List<Attachment>() 
                                { new Attachment() { Url = new Uri("media/@300", UriKind.Relative) }, 
                                  new Attachment() { Url = new Uri("#containedPhoto", UriKind.Relative) },
                                  new Attachment() { Url = new Uri("http://www.nu.nl/fotos/1.jpg", UriKind.Absolute) }
                                };

            var pic300 = new Media();
            pic300.Content = new Attachment() { 
                        Url = new Uri("http://hl7.org/fhir/binary/@300") };
           
            var importer = new ResourceImporter(new Uri("http://hl7.org/fhir"));

            importer.QueueNewResourceEntry("patient", "1", p);
            importer.QueueNewResourceEntry("patient", "10", new Patient());
            importer.QueueNewResourceEntry(new Uri("http://outside.com/fhir/organization/@100"), new Organization());
            importer.QueueNewResourceEntry("media", "300", pic300);

            var result = importer.ImportQueued();

            var p1 = (ResourceEntry<Patient>)result.First();
            Assert.AreEqual("patient/@10", p1.Resource.Link[0].Reference);
            Assert.AreEqual("organization/@11", p1.Resource.Provider.Reference);
            Assert.AreEqual("organization/@200", p1.Resource.Contact.First().Organization.Reference);
            Assert.AreEqual("media/@300", p1.Resource.Photo.First().Url.ToString());
            Assert.AreEqual("#containedPhoto", p1.Resource.Photo.Skip(1).First().Url.ToString());
            Assert.AreEqual("http://www.nu.nl/fotos/1.jpg", p1.Resource.Photo.Skip(2).First().Url.ToString());
            Assert.AreEqual("binary/@300", pic300.Content.Url.ToString());
        }


        [TestMethod]
        public void FullExampleImport()
        {
            ExampleImporter examples = new ExampleImporter();
            examples.ImportZip("examples.zip");

            var importer = new ResourceImporter(new Uri("http://hl7.org/fhir/"));

            foreach (var resourceName in ModelInfo.SupportedResources)
            {
                var key = resourceName.ToLower();
                if (examples.ImportedEntries.ContainsKey(key))
                {
                    var exampleEntries = examples.ImportedEntries[key];

                    foreach (var exampleEntry in exampleEntries)
                        importer.QueueNewEntry(exampleEntry);
                }
            }

            var importedEntries = importer.ImportQueued();
        }

        [TestMethod]
        public void DoExampleInitialize()
        {
            var service = new FhirService(new Uri("http://localhost"));

            service.Initialize();
        }
    }
}
