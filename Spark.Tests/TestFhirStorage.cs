using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Data.MongoDB;
using System.Xml.Linq;
using Hl7.Fhir.Model;
using MongoDB.Driver;
using Spark.Service;
using Hl7.Fhir.Serializers;
using Spark.Data.AmazonS3;
using Spark.Support;
using Hl7.Fhir.Support;
using System.Collections.Specialized;
using Hl7.Fhir.Parsers;
using System.Diagnostics;
using System.IO;

namespace SparkTests
{
    [TestClass]
    public class TestFhirStorage
    {
        private static ResourceEntry _stockPatient;
        private static ResourceEntry _stockOrg;
        private static List<BundleEntry> _stockPatients;
        private static List<BundleEntry> _stockOrgs;
        private static MongoFhirStore _store = null;


        private static ExampleImporter _import = null;

        [ClassInitialize]
        public static void SetupTest(TestContext ctx)
        {
            _import = new ExampleImporter();
            _import.ImportZip("examples.zip");

            _stockPatients = _import.ImportedEntries["patient"];
            _stockOrgs = _import.ImportedEntries["organization"];

            _stockPatient = (ResourceEntry)_stockPatients[0];
            _stockOrg = (ResourceEntry)_stockOrgs[0];

            _store = new MongoFhirStore();
            _store.EraseData();
        }


      
        [TestMethod]
        public void SimpleStoreSpeedTest()
        {
            Stopwatch w = new Stopwatch();

            w.Start();
            _store.EraseData();

            w.Stop();

            Debug.Print("Erasing data took {0} ms", w.ElapsedMilliseconds);

            w.Restart();
            //var errs = new ErrorList();
            //var entries = new List<BundleEntry>();

            //for (var i = 0; i < 500; i++)
            //{
            //    var xml = FhirSerializer.SerializeBundleEntryToXml(_stockPatient);
            //    var copy = FhirParser.ParseBundleEntryFromXml(xml, errs);
            //    var rl = ResourceLocation.Build("patient", i.ToString());
            //    copy.Id = rl.ToUri();
            //    rl.VersionId = "1";
            //    copy.SelfLink = rl.ToUri();
            //    entries.Add(copy);
            //}

            var bundle = loadExamples();

            w.Stop();

            Debug.Print("Loading examples took {0} ms", w.ElapsedMilliseconds);

            w.Restart();

            foreach (var entry in bundle.Entries)
            {
                var rl = new ResourceLocation(entry.Id);
                rl.VersionId = "1";
                entry.SelfLink = rl.ToUri();
            }

            var importer = new ResourceImporter(new Uri("http://localhost"));
            importer.AddSharedIdSpacePrefix("http://hl7.org/fhir/");

            foreach (var be in bundle.Entries)
                importer.QueueNewEntry(be);

            w.Stop();

            Debug.Print("Queueing examples took {0} ms", w.ElapsedMilliseconds);

            w.Restart();

            var entriesToStore = importer.ImportQueued();
            w.Stop();

            Debug.Print("Importing examples took {0} ms", w.ElapsedMilliseconds);

            w.Restart();

            var guid = Guid.NewGuid();
                     
            _store.AddEntries(entriesToStore,guid);

            w.Stop();

            Debug.Print("Storing {0} patients took {1} ms", entriesToStore.Count(), w.Elapsed.Milliseconds);
        }


        private Bundle loadExamples()
        {
           var batch = BundleEntryFactory.CreateBundleWithEntries("Imported examples", new Uri("http://localhost"), "Test Example Loader", null);

            foreach (var resourceName in ModelInfo.SupportedResources)
            {
                var key = resourceName.ToLower();
                if (_import.ImportedEntries.ContainsKey(key))
                {
                    var exampleEntries = _import.ImportedEntries[key];

                    foreach (var exampleEntry in exampleEntries)
                        batch.Entries.Add(exampleEntry);
                }
            }

            return batch;
        }


        [TestMethod]
        public void SimpleStoreAndRetrieve()
        {
            _store.EraseData();

            _store.AddEntry(_stockPatient);

            ResourceEntry readBack = (ResourceEntry)_store.FindEntryById(_stockPatient.Id);

            Assert.IsNotNull(readBack);
            Assert.IsNotNull(readBack.Id);
            Assert.AreEqual(_stockPatient.Id, readBack.Id);
            Assert.IsNotNull(readBack.Links.SelfLink);
            Assert.IsNotNull(readBack.LastUpdated);

            Assert.AreEqual(FhirSerializer.SerializeResourceToJson(_stockPatient.Resource),
                FhirSerializer.SerializeResourceToJson(readBack.Resource));
        }


        [TestMethod]
        public void ListRecordsByResourceType()
        {
            _store.EraseData();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTime stampi = new DateTime(now.Ticks, DateTimeKind.Unspecified);
            stampi = stampi.AddHours(5);

            DateTimeOffset stamp = new DateTimeOffset(stampi, new TimeSpan(5, 0, 0));

            var importer = new ResourceImporter(new Uri("http://localhost"));

            for (int i = 0; i < 5; i++)
            {
                var tst = _stockPatients[i];
                importer.QueueNewEntry(tst);
            }
            for (int i = 0; i < 5; i++)
            {
                var tst = _stockOrgs[i];
                importer.QueueNewEntry(tst);
            }

            importer.QueueNewDeletedEntry("patient", "31415");

            var imported = importer.ImportQueued();
            _store.AddEntries(imported);

            var recentList = _store.ListCollection("patient");
            Assert.AreEqual(5, recentList.Count(), "Should not contain deleted entries");

            var recentOrgList = _store.ListCollection("organization");
            Assert.AreEqual(5, recentOrgList.Count(), "Should not contain patients");

            recentList = _store.ListCollection("patient", includeDeleted: true);
            Assert.AreEqual(6, recentList.Count(), "Should contain deleted entries");

            var recentListWithFilter = _store.ListCollection("patient", limit: 3);
            Assert.AreEqual(3, recentListWithFilter.Count());

            var recentListWithFilter2 = _store.ListCollection("patient", since: now.AddMinutes(-1));
            Assert.AreEqual(5, recentListWithFilter2.Count());

            var recentListWithFilter3 = _store.ListCollection("patient", since: now.AddMinutes(-1), limit: 3);
            Assert.AreEqual(3, recentListWithFilter3.Count());
        }


        private ResourceEntry clone(ResourceEntry e)
        {
            ErrorList err = new ErrorList();
            var json = FhirSerializer.SerializeBundleEntryToJson(e);

            return (ResourceEntry)FhirParser.ParseBundleEntryFromJson(json, err);
        }


        [TestMethod]
        public void UpdateAndCheckHistory()
        {
            _store.EraseData();

            var importer = new ResourceImporter(new Uri("http://localhost"));

            importer.QueueNewEntry(_stockPatient);
            importer.QueueNewEntry(_stockOrg);

            var dd = (ResourceEntry)clone(_stockPatient);

            ((Patient)dd.Resource).Name[0].Text = "Donald Duck";
            dd.Links.SelfLink = null;
            importer.QueueNewEntry(dd);

            importer.QueueNewDeletedEntry(_stockPatient.Id);
            importer.QueueNewDeletedEntry(_stockOrg.Id);

            var imported = importer.ImportQueued();
            var origId = imported.First().Id;

            _store.AddEntries(imported);

            var history = _store.ListVersionsById(origId)
                .OrderBy(be => new ResourceLocation(be.Links.SelfLink).VersionId);

            Assert.IsNotNull(history);
            Assert.AreEqual(3, history.Count());

            Assert.IsTrue(history.All(be => be.Id == origId));

            Assert.IsTrue(history.Last() is DeletedEntry);

            var ver1 = new ResourceLocation(history.First().Links.SelfLink).VersionId;
            var ver2 = new ResourceLocation(history.ElementAt(1).Links.SelfLink).VersionId;
            var ver3 = new ResourceLocation(history.ElementAt(2).Links.SelfLink).VersionId;

            Assert.AreNotEqual(Int32.Parse(ver1), Int32.Parse(ver2));
            Assert.AreNotEqual(Int32.Parse(ver2), Int32.Parse(ver3));
            Assert.AreNotEqual(Int32.Parse(ver1), Int32.Parse(ver3));

            var firstVersionAsEntry = _store.FindVersionByVersionId(history.First().Links.SelfLink);

            //TODO: There's a bug here...the cr+lf in the _stockPatient gets translated to \r\n in the 'firstVersion'.
            //Cannot see this in the stored version though.
            Assert.AreEqual(FhirSerializer.SerializeResourceToJson(_stockPatient.Resource),
                FhirSerializer.SerializeResourceToJson(((ResourceEntry)firstVersionAsEntry).Resource));

            var secondVersionAsEntry = _store.FindVersionByVersionId(history.ElementAt(1).Links.SelfLink);
            Assert.AreEqual("Donald Duck", ((Patient)((ResourceEntry)secondVersionAsEntry).Resource).Name[0].Text);

            var allHistory = _store.ListVersions();
            Assert.AreEqual(5, allHistory.Count());
            Assert.AreEqual(3, allHistory.Count(be => be.Id.ToString().Contains("patient")));
            Assert.AreEqual(2, allHistory.Count(be => be.Id.ToString().Contains("organization")));
            Assert.AreEqual(2, allHistory.Count(be => be is DeletedEntry));
        }


        [TestMethod]
        public void FindSpecificVersionOfResource()
        {
            _store.EraseData();

            _store.AddEntry(_stockPatient);

            ResourceLocation rl = new ResourceLocation(_stockPatient.SelfLink);

            var dd = (ResourceEntry)clone(_stockPatient);

            ((Patient)dd.Resource).Name[0].Given = new string[] { "Donald Duck" };
            rl.VersionId = (Int32.Parse(rl.VersionId) + 1).ToString();
            dd.SelfLink = rl.ToUri();
            _store.AddEntry(dd);

            var specificVersion = _store.FindVersionByVersionId(dd.SelfLink);

            Assert.IsNotNull(specificVersion);
            Assert.AreEqual(specificVersion.SelfLink, dd.SelfLink);
            Assert.AreEqual(specificVersion.Id, dd.Id);
            Assert.AreEqual("Donald Duck", ((Patient)((ResourceEntry)specificVersion).Resource)
                .Name[0].Given.First());

            var originalVersion = _store.FindVersionByVersionId(_stockPatient.Links.SelfLink);
            Assert.IsNotNull(originalVersion);
            Assert.AreEqual("Eve", ((Patient)((ResourceEntry)originalVersion).Resource)
                .Name[0].Given.First());
        }


        //[TestMethod]
        //public void PurgeResource()
        //{
        //    _store.EraseData();

        //    _store.AddEntry(_stockPatient);
        //    _store.AddEntry(_stockPatient);

        //    _store.PurgeAllVersionsById(_stockPatient.Id);

        //    var deletedRecord = _store.FindEntryById(_stockPatient.Id);
        //    Assert.IsNull(deletedRecord);  // should _not_ be found5

        //    _store.AddEntry(_stockPatient);
        //    var second = _store.AddEntry(_stockPatient);

        //    _store.PurgeVersion(second.Links.SelfLink);

        //    var remainingRecords = _store.ListVersionsById(_stockPatient.Id);

        //    Assert.AreEqual(1, remainingRecords.Count());
        //    Assert.AreEqual(_stockPatient.Id, remainingRecords.Last().Id);
        //    Assert.IsTrue(remainingRecords.First().Links.SelfLink != second.Links.SelfLink);
        //}

        [TestMethod]
        public void TestInsertIdHigherThanCounter()
        {
            //TODO: Move this test to wherever this responsability is going to move.

            //var pat = (ResourceEntry)clone(_stockPatient);

            //var rl = new ResourceLocation(pat.Id);
            //rl.Id = "1234";

            //pat.Id = rl.OperationPath;

            //_store.AddEntry(pat);

            //Assert.AreEqual(pat.Id.ToString(), inserted.Id.ToString());

            //int newId = _store.GenerateNewIdSequenceNumber();
            //Assert.AreEqual(1235,newId);

            int i = _store.GenerateNewIdSequenceNumber();
            int j = _store.GenerateNewIdSequenceNumber();

            Assert.IsTrue(j > i);
            Assert.AreNotEqual(0, i);

            _store.EnsureNextSequenceNumberHigherThan(99);
            int k = _store.GenerateNewIdSequenceNumber();
            Assert.AreEqual(100, k);

        }

        [TestMethod]
        public void TestUseAndPurgeBatch()
        {
            _store.EraseData();

            _store.AddEntries(_stockPatients.Take(10));
            var result = _store.ListCollection("patient");
            Assert.AreEqual(10, result.Count());
            _store.EraseData();

            Guid batchGuid = Guid.NewGuid();
            _store.AddEntry(_stockPatients[0], batchId: batchGuid);
            _store.AddEntry(_stockPatients[1], batchId: batchGuid);
            result = _store.ListCollection("patient");
            Assert.AreEqual(2, result.Count());
            _store.PurgeBatch(batchGuid);
            result = _store.ListCollection("patient");
            Assert.AreEqual(0, result.Count());

            batchGuid = Guid.NewGuid();
            _store.AddEntries(_stockPatients.Take(10), batchGuid);
            _store.PurgeBatch(batchGuid);
            result = _store.ListCollection("patient");
            Assert.AreEqual(0, result.Count());
        }


        [TestMethod]
        public void MakeSnapshot()
        {
            _store.EraseData();

            for (int i = 0; i < 5; i++)
            {
                var tst = _stockPatients[i];
                _store.AddEntry(tst);
            }

            var entries = _store.ListCollection("patient");

            var entrySelfLinks = entries.Select(entry => entry.Links.SelfLink);

            var snap = new Snapshot
            {
                Id = Guid.NewGuid().ToString(),
                FeedTitle = "Test snapshot",
                FeedSelfLink = "svc/search?name=Kramer",
                Contents = entrySelfLinks
            };

            _store.StoreSnapshot(snap);

            var snap2 = _store.GetSnapshot(snap.Id);

            CollectionAssert.AreEqual(snap.Contents.ToList(), snap2.Contents.ToList());
            Assert.AreEqual(snap.Id, snap2.Id);
            Assert.AreEqual(snap.FeedTitle, snap2.FeedTitle);
            Assert.AreEqual(snap.FeedSelfLink, snap2.FeedSelfLink);

            var snap3 = _store.FindByVersionIds(snap2.Contents);
            Assert.IsNotNull(snap3);
            Assert.AreEqual(5, snap3.Count());
        }
    }

}

