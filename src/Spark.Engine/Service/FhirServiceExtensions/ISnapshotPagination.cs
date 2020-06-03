using System;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface ISnapshotPagination
    {
        Task<Bundle> GetPage(int? index = null, Action<Entry> transformElement = null);
    }
}
