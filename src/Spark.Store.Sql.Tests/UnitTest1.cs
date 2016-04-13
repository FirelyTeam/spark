using System;
using System.Linq;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Engine.Core;
using Spark.Store.Sql.Model;

namespace Spark.Store.Sql.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            //SqlScopedFhirStore< Project > store = new SqlScopedFhirStore<Project>(new Repository.Repository());
            //store.Scope = new Project() {ScopeKey = 8};
            //Entry e = Entry.POST(new Endpoint(String.Empty,"Patient", "spark7", "spark56"), GetNewPatient());
            //store.Add(e);
        }

        private static Patient GetNewPatient()
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
            selena.Id = "Patient/spark7/spark56";
            return selena;
        }
    }

    public class Project 
    {
        public int ScopeKey { get; set; }
    }
}
