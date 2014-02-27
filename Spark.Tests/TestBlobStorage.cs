using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Service;
using Hl7.Fhir.Support;
using Hl7.Fhir.Model;
using Spark.Store;
using System.IO;
using Spark.Data.MongoDB;
using Spark.Support;

namespace SparkTests
{
    [TestClass]
    public class TestBlobStorage
    {
        private MongoFhirStore _store = null;

        [TestInitialize]
        public void Setup()
        {
            
            _store = new MongoFhirStore();
            _store.EraseData();
        }

        
        [TestMethod]
        public void TestStoreBinaryEntry()
        {
            byte[] data = UTF8Encoding.UTF8.GetBytes("Hello world!");

            var e = new ResourceEntry<Binary>();

            e.AuthorName = "Ewout";
            e.Id = new Uri("binary/@3141", UriKind.Relative);
            e.Links.SelfLink = new Uri("binary/@3141/history/@1", UriKind.Relative);
            e.Resource = new Binary() { Content = data, ContentType = "text/plain" };

            // Test store
            var result = (ResourceEntry<Binary>)_store.AddEntry(e);

            Assert.IsNotNull(result.Resource);
            CollectionAssert.AreEqual(data, result.Resource.Content);
            Assert.AreEqual("text/plain", result.Resource.ContentType);

            // Test retrieve
            ResourceEntry<Binary> result2 = (ResourceEntry<Binary>)_store.FindEntryById(e.Id);
            Assert.IsNotNull(result2);
            CollectionAssert.AreEqual(e.Resource.Content, result2.Resource.Content);
            Assert.AreEqual(e.Resource.ContentType, result2.Resource.ContentType);

            // Test update
            byte[] data2 = UTF8Encoding.UTF8.GetBytes("Hello world!?");
            e.Resource = new Binary() { Content = data2, ContentType = "text/plain" };
            e.SelfLink = new Uri("binary/@3141/history/@2", UriKind.Relative);
            ResourceEntry<Binary> result3 = (ResourceEntry<Binary>)_store.AddEntry(e);
            Assert.IsNotNull(result3);
            Assert.AreEqual(e.Resource.ContentType, result3.Resource.ContentType);
            CollectionAssert.AreEqual(e.Resource.Content, result3.Resource.Content);

            // Test fetch latest
            ResourceEntry<Binary> result4 = (ResourceEntry<Binary>)_store.FindEntryById(e.Id);
            Assert.AreEqual(result4.Id, result3.Id);

            var allVersions = _store.ListVersionsById(e.Id);
            Assert.AreEqual(2, allVersions.Count());
            Assert.AreEqual(allVersions.First().Id, e.Id);
            Assert.AreEqual(allVersions.Skip(1).First().Id, result.Id);
        }


        [TestMethod]
        public void TestStoreStuffOnAmazon()
        {
            var s3 = new AmazonS3Storage();
            s3.Open();

            s3.DeleteAll();
            var names = s3.ListNames();
            Assert.AreEqual(0, names.Length);

            storeString(s3, "blob1", "Hello, world!");
            storeString(s3, "blob2", "Gangnam style");
            storeString(s3, "blob3", "All your base are belong to us");

            names = s3.ListNames();
            CollectionAssert.Contains(names, "blob1");
            CollectionAssert.Contains(names, "blob2");
            CollectionAssert.Contains(names, "blob3");
            Assert.AreEqual(3, names.Length);

            s3.Delete("blob3");
            names = s3.ListNames();
            CollectionAssert.Contains(names, "blob1");
            CollectionAssert.Contains(names, "blob2");
            Assert.AreEqual(2, names.Length);

            s3.Delete("blob3");
            // No exception, please
            names = s3.ListNames();
            CollectionAssert.Contains(names, "blob1");
            CollectionAssert.Contains(names, "blob2");
            Assert.AreEqual(2, names.Length);

            storeString(s3, "blob3", "20 Return 10"); 
            storeString(s3, "blob3", "10 Goto 20");
            // No exception, please
            string contentType;
            byte[] data = s3.Fetch("blob3", out contentType);
            string output = Encoding.UTF8.GetString(data);

            Assert.AreEqual("10 Goto 20", output);

            names = s3.ListNames();
            CollectionAssert.Contains(names, "blob1");
            CollectionAssert.Contains(names, "blob2");
            CollectionAssert.Contains(names, "blob3");
            Assert.AreEqual(3, names.Length);

            s3.Close();
        }

        private void storeString(AmazonS3Storage s3, string key, string value)
        {
            MemoryStream mems = new MemoryStream();
            Encoding enc = new UTF8Encoding(false);
            StreamWriter writer = new StreamWriter(mems, enc);

            writer.Write(value); writer.Flush();
            s3.Store(key, mems, "text/plain");
        }     
  
        [TestMethod]
        public void TestBatchRemoval()
        {
            _store.EraseData();

            byte[] data = UTF8Encoding.UTF8.GetBytes("Hello world!");
            ResourceEntry<Binary> e = new ResourceEntry<Binary>();

            e.AuthorName = "Ewout";


            e.Resource = new Binary { Content = data, ContentType = "text/plain" };

            var rl = ResourceLocation.Build("binary", "3141");
            e.Id = rl.ToUri();
            rl.VersionId = "1";
            e.SelfLink = rl.ToUri();
            var batchGuid = Guid.NewGuid();

            ResourceEntry<Binary> result = (ResourceEntry<Binary>)_store.AddEntry(e, batchGuid);

            // Store 5 others with another batchguid
            batchGuid = Guid.NewGuid();

            for (int i = 0; i < 5; i++)
            {
                rl = ResourceLocation.Build("binary", (10+i).ToString());
                e.Id = rl.ToUri();
                rl.VersionId = "1";
                e.SelfLink = rl.ToUri();

                result = (ResourceEntry<Binary>)_store.AddEntry(e, batchGuid);
            }

            _store.PurgeBatch(batchGuid);

            var result2 = _store.ListCollection("binary");
            Assert.AreEqual(1, result2.Count());
            Assert.AreEqual("3141", ResourceLocation.GetIdFromResourceId(result2.First().Id));

            using (var s3 = new AmazonS3Storage())
            {
                s3.Open();
                var names = s3.ListNames();
                Assert.AreEqual(1, names.Length);
                Assert.IsTrue(names.First().Contains("3141"));
            }
        }
    }
}
