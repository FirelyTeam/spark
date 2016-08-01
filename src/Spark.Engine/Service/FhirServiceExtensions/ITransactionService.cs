using System;
using System.Collections.Generic;
using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Service;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface ITransactionService : IFhirServiceExtension
    {
        IList<Tuple<Entry, FhirResponse>> HandleTransaction(Bundle bundle);
        IList<Tuple<Entry, FhirResponse>> HandleTransaction(IList<Entry> interactions);
        IFhirService FhirService { get; set; }
    }
}