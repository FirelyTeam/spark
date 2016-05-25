using System.Collections.Generic;
using Hl7.Fhir.Model;
using Spark.Engine.Core;

namespace Spark.Service
{
    public interface ITransfer
    {
        void Externalize(IEnumerable<Entry> interactions);
        void Externalize(Entry interaction);
        void Internalize(IEnumerable<Entry> interactions);
        void Internalize(Entry entry);
        void Externalize(Bundle bundle);
    }
}