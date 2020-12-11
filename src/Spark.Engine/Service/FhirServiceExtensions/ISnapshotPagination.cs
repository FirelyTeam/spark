using System;
using Hl7.Fhir.Model;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    using System.Threading.Tasks;

    public interface ISnapshotPagination
    {
        Task<Bundle> GetPage(int? index = null, Action<Entry> transformElement = null);
    }
}