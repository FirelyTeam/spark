using System;
using System.Collections.Generic;
using Hl7.Fhir.Model;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface ITransactionService : IFhirServiceExtension
    {
        FhirResponse HandleTransaction(ResourceManipulationOperation operation, IInteractionHandler interactionHandler);
        IList<Tuple<Entry, FhirResponse>> HandleTransaction(Bundle bundle, IInteractionHandler interactionHandler);
        IList<Tuple<Entry, FhirResponse>> HandleTransaction(IList<Entry> interactions, IInteractionHandler interactionHandler);
    }
}