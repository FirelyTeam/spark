using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface ITransactionService : IFhirServiceExtension
    {
        [Obsolete("Use HandleTransactionAsync(ResourceManipulationOperation, IInteractionHandler) instead")]
        FhirResponse HandleTransaction(ResourceManipulationOperation operation, IInteractionHandler interactionHandler);
        [Obsolete("Use HandleTransactionAsync(Bundle, IInteractionHandler) instead")]
        IList<Tuple<Entry, FhirResponse>> HandleTransaction(Bundle bundle, IInteractionHandler interactionHandler);
        [Obsolete("Use HandleTransactionAsync(IList<Entry>, IInteractionHandler) instead")]
        IList<Tuple<Entry, FhirResponse>> HandleTransaction(IList<Entry> interactions, IInteractionHandler interactionHandler);
        Task<FhirResponse> HandleTransactionAsync(ResourceManipulationOperation operation, IInteractionHandler interactionHandler);
        Task<IList<Tuple<Entry, FhirResponse>>> HandleTransactionAsync(Bundle bundle, IInteractionHandler interactionHandler);
        Task<IList<Tuple<Entry, FhirResponse>>> HandleTransactionAsync(IList<Entry> interactions, IInteractionHandler interactionHandler);
    }
}