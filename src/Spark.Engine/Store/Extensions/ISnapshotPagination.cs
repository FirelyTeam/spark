using System;
using Hl7.Fhir.Model;
using Spark.Engine.Core;

namespace Spark.Engine.Storage.StoreExtensions
{
    public interface ISnapshotPagination
    {
        Bundle GetPage(int index, Action<Entry> transformElement = null);
    }
}