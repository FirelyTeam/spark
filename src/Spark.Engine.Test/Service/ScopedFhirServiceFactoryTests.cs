using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Spark.Engine.Core;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Interfaces;
using Spark.Engine.Service;
using Spark.Service;

namespace Spark.Engine.Test.Service
{
    [TestClass]
    public class ScopedFhirServiceFactoryTests
    {
        private ScopedFhirServiceFactory fhirServiceFactory;

        [TestInitialize]
        public void Test_Initialize()
        {
            Mock<IBaseFhirResponseFactory> responseFactory = new Mock<IBaseFhirResponseFactory>();
            Mock<ITransfer> transfer = new Mock<ITransfer>();
            fhirServiceFactory = new ScopedFhirServiceFactory(responseFactory.Object, transfer.Object);
           // fhirServiceFactory.RegisterStore<Project, FakeFhirStore<Project>>();

        }

        [TestMethod]
        public void GetFhirService_RegisteredScopedForProject()
        {
            IFhirService fhirService = fhirServiceFactory.GetFhirService(new Project());
            Assert.IsNotNull(fhirService);
        }

    }

    //public class FakeFhirStore<T> : IScopedFhirStore<T>
    //{
    //    public void AddExtension<T1>(T1 extension) where T1 : IFhirStoreExtension
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public void RemoveExtension<T1>() where T1 : IFhirStoreExtension
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public T1 FindExtension<T1>() where T1 : IFhirStoreExtension
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public void Add(Entry entry)
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public Entry Get(IKey key)
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public IList<Entry> Get(IEnumerable<string> identifiers, string sortby)
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public T Scope { get; set; }
    //}
  
}