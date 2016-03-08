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
    public class GenericScopedFhirServiceFactoryTests
    {
        private GenericScopedFhirServiceFactory<Project> fhirServiceFactory;

        [TestInitialize]
        public void Test_Initialize()
        {
            Mock<IScopedFhirStoreBuilder<Project>> storeBuilder = new Mock<IScopedFhirStoreBuilder<Project>>();
            Mock<IBaseFhirResponseFactory> responseFactory = new Mock<IBaseFhirResponseFactory>();
            Mock<ITransfer> transfer = new Mock<ITransfer>();
            fhirServiceFactory = new GenericScopedFhirServiceFactory<Project>(storeBuilder.Object, responseFactory.Object, transfer.Object);
        }

        [TestMethod]
        public void GetFhirService_ReturnsFhirService()
        {
            Project project = new Project();
            IFhirService fhirService = fhirServiceFactory.GetFhirService(project);

            Assert.IsNotNull(fhirService);
        }
    }

    public class Project
    {

    }
}