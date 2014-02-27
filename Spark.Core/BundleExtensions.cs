using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Spark.Core
{
    public static class BundleExtensions
    {
        public static IEnumerable<Uri> SelfLinks(this Bundle bundle)
        {
            return bundle.Entries.Select(entry => entry.Links.SelfLink);
        }
    }
}