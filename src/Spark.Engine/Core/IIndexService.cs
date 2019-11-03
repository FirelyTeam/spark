using Hl7.Fhir.Model;
using Spark.Engine.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Spark.Engine.Core
{
    public interface IIndexService
    {
        void Process(Entry entry);
        IndexValue IndexResource(Resource resource, IKey key);
    }
}
