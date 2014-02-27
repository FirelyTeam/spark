using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Spark.Search
{
    public interface IIndexer
    {
        void Put(ResourceEntry entry);
        void Put(IEnumerable<ResourceEntry> entries);
        void Delete(DeletedEntry entry);
        void Delete(IEnumerable<DeletedEntry> entries);
        void Clean();
    }
}