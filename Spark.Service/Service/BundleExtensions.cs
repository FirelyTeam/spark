using Hl7.Fhir.Model;
using Spark.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Service
{
    public static class BundleExtensions
    {
        public static IEnumerable<Uri> GetReferences(this BundleEntry entry, string include)
        {
            Resource resource = (entry as ResourceEntry).Resource;
            ElementQuery query = new ElementQuery(include);
            var list = new List<Uri>();

            query.Visit(resource, element =>
            {
                if (element is ResourceReference)
                {
                    Uri uri = (element as ResourceReference).Url;
                    if (uri != null) list.Add(uri);
                }
            });
            return list.Where(u => u != null);
        }

        public static IEnumerable<Uri> GetReferences(this Bundle bundle, string include)
        {
            foreach (BundleEntry entry in bundle.Entries)
            {
                IEnumerable<Uri> list = GetReferences(entry, include);
                foreach (Uri value in list)
                {
                    if (value != null)
                        yield return value;
                }
            }
        }

        public static IEnumerable<Uri> GetReferences(this Bundle bundle, IEnumerable<string> includes)
        {
            return includes.SelectMany(include => GetReferences(bundle, include));
        }

        public static void AddRange(this Bundle bundle, IEnumerable<BundleEntry> entries)
        {
            foreach (BundleEntry entry in entries)
            {
                bundle.Entries.Add(entry);
            }
        }
    }
}
