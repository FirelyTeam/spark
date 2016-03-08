using System;
using Hl7.Fhir.Model;
using Spark.Engine.Core;

namespace Spark.Engine.Interfaces
{
    public interface IHistoryExtension : IFhirStoreExtension
    {
        Bundle History(string typename, HistoryParameters parameters);
        Bundle History(IKey key, HistoryParameters parameters);
        Bundle History(HistoryParameters parameters);
    }
}