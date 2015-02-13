using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Hl7.Fhir.Model;

namespace Spark.Controllers
{
    public static class Test
    {
        public static Patient Patient()
        {
            Patient patient = new Patient();
            patient.Id = "patient/1";
            HumanName h = new HumanName().WithGiven("Martijn");
            patient.Name = new List<HumanName>();
            patient.Name.Add(h);
            return patient;
        }

        public  static Bundle Bundle()
        {
            Bundle bundle = new Bundle();
            Patient p = Test.Patient();
            BundleEntry entry = ResourceEntry.Create(p);
            bundle.Entries.Add(entry);
            return bundle;
        }
    }
}