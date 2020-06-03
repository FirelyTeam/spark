﻿using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Engine.Extensions;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface IPagingService : IFhirServiceExtension
    {
        ISnapshotPagination StartPagination(Snapshot snapshot);
        Task<ISnapshotPagination> StartPagination(string snapshotkey);
    }
}
