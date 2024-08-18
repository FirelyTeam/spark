/* 
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Spark.Engine.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface ITransactionService : IFhirServiceExtension
    {
        Task<FhirResponse> HandleTransactionAsync(ResourceManipulationOperation operation, IInteractionHandler interactionHandler);
        Task<IList<Tuple<Entry, FhirResponse>>> HandleTransactionAsync(Bundle bundle, IInteractionHandler interactionHandler);
        Task<IList<Tuple<Entry, FhirResponse>>> HandleTransactionAsync(IList<Entry> interactions, IInteractionHandler interactionHandler);
    }
}
