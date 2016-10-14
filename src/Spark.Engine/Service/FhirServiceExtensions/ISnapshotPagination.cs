using System;
using Hl7.Fhir.Model;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface ISnapshotPagination
    {
        Bundle GetPage(int? index = null, Action<Entry> transformElement = null);
    }
}