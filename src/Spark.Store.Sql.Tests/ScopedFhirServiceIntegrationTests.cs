using System;
using System.Linq;
using System.Net;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Engine.Core;
using Spark.Engine.Service;
using Spark.Store.Sql.Contracts;
using Spark.Store.Sql.Model;

namespace Spark.Store.Sql.Tests
{
    //[TestClass]
    //public class ScopedFhirServiceIntegrationTests
    //{
    //    private SqlScopedFhirService<Project> serviceProject;
    //    private Project project1;
    //    private Project project2;
    //    [TestInitialize]
    //    public void TestInitialize()
    //    {
    //        Uri uri = new Uri("http://localhost:49911/fhir", UriKind.Absolute);
    //        FhirDefaultDbContext context = new FhirDefaultDbContext("IFhirDbContext");
    //        int x= context.ResourceVersions.Count();

    //        SqlScopedFhirServiceFactory factory = new SqlScopedFhirServiceFactory();
    //        serviceProject = factory.GetFhirService<Project>(context, uri, p => p.ScopeKey);

    //        project1 = new Project() {ScopeKey = 1};
    //        project2 = new Project() {ScopeKey = 2};

    //    }

    //    [TestMethod]
    //    public void ScopedFhirService_AddResource_GetResourceReturnsSameResource()
    //    {
    //        //create patient
    //        Key patientKey = new Key(String.Empty, "Patient", null, null);
    //        Patient patient = GetNewPatient(patientKey);
    //        FhirResponse response = serviceProject.WithEntity(CreateResourceContent()).WithScope(project1).Create(patientKey, patient);

    //        //read created patient
    //        response = serviceProject.WithScope(project1).Read(response.Resource.ExtractKey().WithoutVersion());
    //        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

    //        //check correctness of read patient
    //        patient = (Patient) response.Resource;
    //        Assert.AreEqual(AdministrativeGender.Female, patient.Gender);

    //        //update patient
    //        patient.Gender = AdministrativeGender.Male;
    //        response = serviceProject.WithScope(project1).Update(patient.ExtractKey(), patient);
    //        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    //        patient = (Patient)response.Resource;
    //        Assert.AreEqual(AdministrativeGender.Male, patient.Gender);

    //        //read patient in different project
    //        response = serviceProject.WithScope(project2).Read(response.Resource.ExtractKey().WithoutVersion());
    //        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

    //        //search for patient in the correct project
    //        response = serviceProject.WithScope(project1).Search("Patient", new SearchParams());
    //        Assert.AreEqual(1, ((Bundle)response.Resource).TotalElement.Value);

    //        //search for patient in different project
    //        response = serviceProject.WithScope(project2).Search("Patient", new SearchParams());
    //        Assert.AreEqual(0, ((Bundle)response.Resource).TotalElement.Value);

    //        ////delete patient
    //        //response = serviceProject.WithScope(project1).Delete(patient.ExtractKey().WithoutVersion());
    //        //Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

    //        ////search patient again in correct project
    //        //response = serviceProject.WithScope(project1).Read(patient.ExtractKey().WithoutVersion());
    //        //Assert.AreEqual(HttpStatusCode.Gone, response.StatusCode);

    //        ////search for patient again in correct project
    //        //response = serviceProject.WithScope(project1).Search("Patient", new SearchParams());
    //        //Assert.AreEqual(0, ((Bundle)response.Resource).TotalElement.Value);
    //    }

    //    private ResourceContent CreateResourceContent()
    //    {
    //        return new ConcreteResourceContent()
    //        {
    //            Resource = new ConcreteResource()
    //        };
    //    }

    //    [TestMethod]
    //    public void ScopedFhirService_TestHistory()
    //    {
    //        //create patient
    //        Key patientKey = new Key(String.Empty, "Patient", null, null);
    //        Patient patient = GetNewPatient(patientKey);
    //        FhirResponse response = serviceProject.WithScope(project1).Create(patientKey, patient);


    //        //read history
    //        response = serviceProject.WithScope(project1).History("Patient", new HistoryParameters());
    //        Assert.AreEqual(1, ((Bundle) response.Resource).TotalElement.Value);

    //        patientKey = ((Bundle) response.Resource).Entry[0].Resource.ExtractKey();

    //        response = serviceProject.WithScope(project1).History(patientKey, new HistoryParameters());
    //        Assert.AreEqual(1, ((Bundle) response.Resource).TotalElement.Value);

    //        response = serviceProject.WithScope(project1).History(new HistoryParameters());
    //        Assert.AreEqual(1, ((Bundle) response.Resource).TotalElement.Value);

    //        response = serviceProject.WithScope(project1).History(new HistoryParameters() {Since = DateTimeOffset.Now.AddHours(1).ToUniversalTime() });
    //        Assert.AreEqual(0, ((Bundle) response.Resource).TotalElement.Value);

    //        serviceProject.WithScope(project1).Delete(patientKey.WithoutVersion());
    //    }


    //    private static Patient GetNewPatient(Key key)
    //    {
    //        Patient selena = new Patient();

    //        var name = new HumanName();
    //        name.GivenElement.Add(new FhirString("Selena"));
    //        name.FamilyElement.Add(new FhirString("Gomez"));
    //        selena.Name.Add(name);

    //        var address = new Address();
    //        address.LineElement.Add(new FhirString("Cornett"));
    //        address.CityElement = new FhirString("Amanda");
    //        address.CountryElement = new FhirString("United States");
    //        address.StateElement = new FhirString("Texas");
    //        selena.Address.Add(address);

    //        var contact = new Patient.ContactComponent();
    //        var contactname = new HumanName();
    //        contactname.GivenElement.Add(new FhirString("Martijn"));
    //        contactname.FamilyElement.Add(new FhirString("Harthoorn"));
    //        contact.Name = contactname;
    //        selena.Contact.Add(contact);

    //        selena.Gender = AdministrativeGender.Female;
    //        selena.Id = key.ToString();
    //        return selena;
    //    }
    //}


}