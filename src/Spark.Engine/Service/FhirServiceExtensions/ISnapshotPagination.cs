using System;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface ISnapshotPagination
    {
        [Obsolete("Use GetPageAsync(int?, Action<Entry>) instead")]
        Bundle GetPage(int? index = null, Action<Entry> transformElement = null);
        Task<Bundle> GetPageAsync(int? index = null, Action<Entry> transformElement = null);
    }
}