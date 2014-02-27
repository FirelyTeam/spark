using System;
using Hl7.Fhir.Model;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Spark.Core
{
    public interface IFhirIndex
    {
        void Clean();
        void Delete(DeletedEntry entry);
        //SearchResults Filter(string resourcename, string fieldname, string value);
        //SearchResults LuceneQuery(string query);
        void Process(Bundle bundle);
        void Process(BundleEntry entry);
        void Process(IEnumerable<BundleEntry> bundle);
        //void Put(ContentEntry entry);
        //SearchResults Search(Parameters parameters);
        SearchResults Search(string resource, IEnumerable<Tuple<string, string>> collection);
        SearchResults Search(string resourcename, string query = "");
    }
}
