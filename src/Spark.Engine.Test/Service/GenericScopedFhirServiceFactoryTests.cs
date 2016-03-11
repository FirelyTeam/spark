using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Service;
using Spark.Service;

namespace Spark.Engine.Test.Service
{
    [TestClass]
    public class GenericScopedFhirServiceFactoryTests
    {
        private GenericScopedFhirServiceFactory<Project> fhirServiceFactory;

        //[TestInitialize]
        //public void Test_Initialize()
        //{
        //    IScopedFhirStoreBuilder<Project> builder 
        //    Mock<IScopedFhirStoreBuilder<Project>> storeBuilder = new Mock<IScopedFhirStoreBuilder<Project>>();
        //    Mock<IBaseFhirResponseFactory> responseFactory = new Mock<IBaseFhirResponseFactory>();
        //    Mock<ITransfer> transfer = new Mock<ITransfer>();
        //    fhirServiceFactory = new GenericScopedFhirServiceFactory<Project>(storeBuilder.Object, responseFactory.Object, transfer.Object);
        //}

        //[TestMethod]
        //public void GetFhirService_ReturnsFhirService()
        //{
        //    Project project = new Project();
        //    IFhirService fhirService = fhirServiceFactory.GetFhirService(project);

        //    Assert.IsNotNull(fhirService);
        //}
    }

    public class Project 
    {

    }
}