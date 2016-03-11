using System;
using System.Net;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Engine.Core;
using Spark.Engine.Service;
using Spark.Service;

namespace Spark.Store.Sql.Tests
{
    [TestClass]
    public class ScopedFhirServiceIntegrationTests
    {
        private IFhirService serviceProject1;
        private IFhirService serviceProject2;
        [TestInitialize]
        public void TestInitialize()
        {
            Uri uri = new Uri("http://localhost:49911/fhir", UriKind.Absolute);
            GenericScopedFhirServiceFactory<Project> factory = new GenericScopedFhirServiceFactory<Project>(new SqlScopedFhirStoreBuilder<Project>());
            serviceProject1 = factory.GetFhirService(uri, new Project() { ScopeKey = 1 });
            serviceProject2 = factory.GetFhirService(uri, new Project() {ScopeKey = 2});
        }

        [TestMethod]
        public void ScopedFhirService_AddResource_GetResourceReturnsSameResource()
        {
            Key patientKey = new Key(String.Empty, "Patient", null, null);
            FhirResponse response= serviceProject1.Create(patientKey, GetNewPatient(patientKey));
            response = serviceProject1.Read(response.Resource.ExtractKey().WithoutVersion());
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            response = serviceProject2.Read(response.Resource.ExtractKey().WithoutVersion());
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

            response = serviceProject1.Search("Patient", new SearchParams());
            Assert.AreEqual(1, ((Bundle) response.Resource).Total);

            response = serviceProject2.Search("Patient", new SearchParams());
            Assert.AreEqual(0, ((Bundle)response.Resource).Total);
        }


        private static Patient GetNewPatient(Key key)
        {
            Patient selena = new Patient();

            var name = new HumanName();
            name.GivenElement.Add(new FhirString("Selena"));
            name.FamilyElement.Add(new FhirString("Gomez"));
            selena.Name.Add(name);

            var address = new Address();
            address.LineElement.Add(new FhirString("Cornett"));
            address.CityElement = new FhirString("Amanda");
            address.CountryElement = new FhirString("United States");
            address.StateElement = new FhirString("Texas");
            selena.Address.Add(address);

            var contact = new Patient.ContactComponent();
            var contactname = new HumanName();
            contactname.GivenElement.Add(new FhirString("Martijn"));
            contactname.FamilyElement.Add(new FhirString("Harthoorn"));
            contact.Name = contactname;
            selena.Contact.Add(contact);

            selena.Gender = AdministrativeGender.Female;
            selena.Id = key.ToString();
            return selena;
        }
    }

   
}